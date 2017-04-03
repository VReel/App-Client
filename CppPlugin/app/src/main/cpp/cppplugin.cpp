#include <jni.h>
#include <string>
#include <cstdio>
#include <android/log.h>
#include <GLES2/gl2.h>
#include "Unity/IUnityGraphics.h"
#define STB_IMAGE_IMPLEMENTATION
#include "stb_image.h"

#define  LOG_TAG    "----------------- VREEL: CppPlugin - "
#define  LOGI(...) __android_log_print(ANDROID_LOG_INFO,LOG_TAG,__VA_ARGS__)

// **************************
// Member Variables
// **************************

int m_numInits = 0; // Acts a bit like a reference counter, ensuring only 1 Init() and 1 Terminate()

GLuint* m_textureIDs;
int m_initMaxNumTextures = 0; // Set on Init - sets maximum textures to gen!
int m_currTextureIndex = 0;

const int kMaxImageWidth = 16 * 1024;
const int kMaxImageHeight = 8 * 1024;
stbi_uc* m_pWorkingMemory = NULL;
int m_currImageWidth = 0;
int m_currImageHeight = 0;

int m_maxPixelsUploadedPerFrame = 1 * 1024 * 1024;
bool m_isLoadingIntoTexture = false;
GLint m_textureLoadingYOffset = 0;

// **************************
// Helper functions
// **************************

static void PrintGLString(const char *name, GLenum s)
{
    const char *v = (const char *) glGetString(s);
    LOGI("GL %s = %s\n", name, v);
}

static void PrintAllGlError()
{
    LOGI("Printing all GL Errors:\n");
    for (GLint error = glGetError(); error; error = glGetError())
    {
        LOGI("  glError (0x%x)\n", error);
    }
}

static void CheckGlError(const char* op)
{
    for (GLint error = glGetError(); error; error = glGetError())
    {
        LOGI("after %s() glError (0x%x)\n", op, error);
    }
}

// Image pixels coming from stb_image.h are upside-down and back-to-front, this function corrects that
void TransferAndCorrectAlignmentFromSrcToDest(int* pImage, int* pDest, int width, int height)
{
    int numPixels = width*height;
    for(int* pSrc = pImage + (numPixels-1); pSrc >= pImage; pSrc -= width)
    {
        for (int* pScanLine = pSrc - (width-1); pScanLine <= pSrc; ++pScanLine)
        {
            *pDest = *pScanLine;
            ++pDest;
        }
    }
}

// **************************
// Private functions - accessed through OnRenderEvent()
// **************************

// These are functions that use OpenGL and hence must be run from the Render Thread, through C#'s GL.IssuePluginEvent()
enum RenderFunctions
{
    kInit = 0,
    kCreateEmptyTexture = 1,
    kLoadScanlinesIntoTextureFromWorkingMemory = 2,
    kTerminate = 3,

    kFBO = 4
};

void Init()
{
    if (m_numInits == 0)
    {
        LOGI("Calling Init()!");

        LOGI("glGenTextures(%d, m_textureIDs)", m_initMaxNumTextures);
        m_textureIDs = new GLuint[m_initMaxNumTextures];
        glGenTextures(m_initMaxNumTextures, m_textureIDs);
        PrintAllGlError();

        for (int i = 0; i < m_initMaxNumTextures; i++)
        {
            LOGI("Genned texture to Handle = %u \n", m_textureIDs[i]);
        }

        m_pWorkingMemory = new stbi_uc[kMaxImageWidth * kMaxImageHeight * sizeof(int32_t)];

        LOGI("Finished Init()!");
    }

    m_numInits++;
}

void Terminate()
{
    m_numInits--;

    if (m_numInits == 0)
    {
        LOGI("Calling Terminate()!");

        LOGI("glDeleteTextures(%d, m_textureIDs)", m_initMaxNumTextures);
        glDeleteTextures(m_initMaxNumTextures, m_textureIDs);
        PrintAllGlError();

        delete[] m_textureIDs;
        delete[] m_pWorkingMemory;

        LOGI("Finished Terminate()!");
    }
}

void CreateEmptyTexture()
{
    LOGI("Calling CreateEmptyTexture()");

    LOGI("glBindTexture(GL_TEXTURE_2D, textureId)");
    GLuint textureId = m_textureIDs[m_currTextureIndex];
    glBindTexture(GL_TEXTURE_2D, textureId);
    PrintAllGlError();

    LOGI("glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, m_imageWidth, m_imageHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, NULL");
    glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, m_currImageWidth, m_currImageHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, NULL); //(unsigned char*) m_pWorkingMemory);
    PrintAllGlError();

    m_isLoadingIntoTexture = true;
    m_textureLoadingYOffset = 0;

    LOGI("Finished CreateEmptyTexture()!");
}

// This function is called repeatedly like a for-loop with the variable m_textureLoadingYOffset updating every iteration
void LoadScanlinesIntoTextureFromWorkingMemory()
{
    LOGI("Calling LoadScanlinesIntoTextureFromWorkingMemory()");

    LOGI("glBindTexture(GL_TEXTURE_2D, textureId)");
    GLuint textureId = m_textureIDs[m_currTextureIndex];
    glBindTexture(GL_TEXTURE_2D, textureId);
    PrintAllGlError();

    // Each iteration we upload up to kMaxPixelsPerUpload worth of width-long scanlines,
    //  up until the last one where we only upload the remaining scanlines
    const GLint kIdealNumberOfScanlinesToUpload = m_maxPixelsUploadedPerFrame/m_currImageWidth;
    GLsizei height = (m_textureLoadingYOffset + kIdealNumberOfScanlinesToUpload < m_currImageHeight)
                     ? kIdealNumberOfScanlinesToUpload
                     : (m_currImageHeight - m_textureLoadingYOffset);

    LOGI("glTexSubImage2D(GL_TEXTURE_2D, 0, 0, %d, m_imageWidth, %d, GL_RGBA, GL_UNSIGNED_BYTE, pImage", m_textureLoadingYOffset, height);
    unsigned int* pImage = (unsigned int*) m_pWorkingMemory + (m_textureLoadingYOffset * m_currImageWidth);
    glTexSubImage2D(GL_TEXTURE_2D, 0, 0, m_textureLoadingYOffset, m_currImageWidth, height, GL_RGBA, GL_UNSIGNED_BYTE, pImage);
    PrintAllGlError();

    m_textureLoadingYOffset += kIdealNumberOfScanlinesToUpload;
    if (m_textureLoadingYOffset > m_currImageHeight)
    {
        m_isLoadingIntoTexture = false;
    }

    LOGI("Finished LoadScanlinesIntoTextureFromWorkingMemory()! Loading in progress = %d", m_isLoadingIntoTexture);
}




void MyFBO()
{
    LOGI("Calling MyFBO()!");

    LOGI("Setup FBO");
    GLuint textureId = m_textureIDs[m_currTextureIndex];
    GLuint FFrameBuffer = 0;
    glGenFramebuffers( 1, &FFrameBuffer );
    glBindFramebuffer( GL_FRAMEBUFFER, FFrameBuffer );
    glFramebufferTexture2D( GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, textureId, 0 );
    glBindFramebuffer( GL_FRAMEBUFFER, 0 );
    PrintAllGlError();

    LOGI("Render to FBO");
    glBindFramebuffer( GL_FRAMEBUFFER, FFrameBuffer );
    glViewport( 0, 0, m_currImageWidth, m_currImageHeight );
    //your rendering code goes here - it will draw directly into the texture
    glBindFramebuffer( GL_FRAMEBUFFER, 0 );
    PrintAllGlError();

    LOGI("Cleanup FBO");
    glBindFramebuffer( GL_FRAMEBUFFER, 0 ); // Render to screen? If so I don't need it
    glDeleteFramebuffers( 1, &FFrameBuffer );
    PrintAllGlError();

    /*
    // The depth buffer
    GLuint depthrenderbuffer;
    glGenRenderbuffers(1, &depthrenderbuffer);
    glBindRenderbuffer(GL_RENDERBUFFER, depthrenderbuffer);
    glRenderbufferStorage(GL_RENDERBUFFER, GL_DEPTH_COMPONENT, 1024, 768);
    glFramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_RENDERBUFFER, depthrenderbuffer);



     // Set the list of draw buffers.
    GLenum DrawBuffers[1] = {GL_COLOR_ATTACHMENT0};
    glDrawBuffers(1, DrawBuffers); // "1" is the size of DrawBuffers
     */
}




static void UNITY_INTERFACE_API OnRenderEvent(int eventID)
{
    if (eventID == kInit)
    {
        Init();
    }
    else if (eventID == kCreateEmptyTexture)
    {
        CreateEmptyTexture();
    }
    else if (eventID == kLoadScanlinesIntoTextureFromWorkingMemory)
    {
        LoadScanlinesIntoTextureFromWorkingMemory();
    }
    else if (eventID == kTerminate)
    {
        Terminate();
    }

    else if (eventID == kFBO)
    {
        MyFBO();
    }
}

// **************************
// Public functions
// **************************

extern "C"
{

UnityRenderingEvent GetRenderEventFunc()
{
    return OnRenderEvent;
}

void SetInitMaxNumTextures(int initMaxNumTextures)
{
    m_initMaxNumTextures = initMaxNumTextures;
}

void SetMaxPixelsUploadedPerFrame(int maxPixelsUploadedPerFrame)
{
    m_maxPixelsUploadedPerFrame = maxPixelsUploadedPerFrame;
}

void SetCurrTextureIndex(int currTextureIndex)
{
    m_currTextureIndex = currTextureIndex;
}

bool IsLoadingIntoTexture()
{
    return m_isLoadingIntoTexture;
}

void* GetCurrStoredTexturePtr()
{
    return (void*)(m_textureIDs[m_currTextureIndex]);
}

int GetCurrStoredImageWidth()
{
    return m_currImageWidth;
}

int GetCurrStoredImageHeight()
{
    return m_currImageHeight;
}

bool LoadIntoWorkingMemoryFromImagePath(char* pFileName)
{
    LOGI("Calling LoadIntoWorkingMemoryFromImagePath()");

    int type = -1;
    m_currImageWidth = m_currImageHeight = 0;

    stbi_uc* pImage = stbi_load(pFileName, &m_currImageWidth, &m_currImageHeight, &type, 4); // Forcing 4-components per pixel RGBA
    TransferAndCorrectAlignmentFromSrcToDest((int*) pImage, (int*) m_pWorkingMemory, m_currImageWidth, m_currImageHeight);
    stbi_image_free(pImage);

    LOGI("Image Loaded has Width = %d, Height = %d, Type = %d\n", m_currImageWidth, m_currImageHeight, type);
    if (m_currImageWidth * m_currImageHeight > kMaxImageWidth * kMaxImageHeight)
    {
        LOGI("ERROR - Image Loaded is greater than Working Memory!!!");
    }

    LOGI("Finished LoadIntoWorkingMemoryFromImagePath()!");

    return (m_currImageWidth * m_currImageHeight) > 0;
}

bool LoadIntoWorkingMemoryFromImageData(void* pRawData, int dataLength)
{
    LOGI("Calling LoadIntoWorkingMemoryFromImageData()");

    int type = -1;
    m_currImageWidth = m_currImageHeight = 0;

    stbi_uc* pImage = stbi_load_from_memory((stbi_uc*) pRawData, dataLength, &m_currImageWidth, &m_currImageHeight, &type, 4); // Forcing 4-components per pixel RGBA
    TransferAndCorrectAlignmentFromSrcToDest((int*) pImage, (int*) m_pWorkingMemory, m_currImageWidth, m_currImageHeight);
    stbi_image_free(pImage);

    LOGI("Image Loaded has Width = %d, Height = %d, Type = %d\n", m_currImageWidth, m_currImageHeight, type);
    if (m_currImageWidth * m_currImageHeight > kMaxImageWidth * kMaxImageHeight)
    {
        LOGI("ERROR - Image Loaded is greater than Working Memory!!!");
    }

    LOGI("Finished LoadIntoWorkingMemoryFromImageData()!");

    return (m_currImageWidth * m_currImageHeight) > 0;
}

jstring Java_com_soul_cppplugin_MainActivity_stringFromJNI(JNIEnv *env, jobject /* this */)
{
    std::string hello = "Hello from C++!";
    return env->NewStringUTF(hello.c_str());
}

}