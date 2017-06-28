#include <jni.h>
#include <string>
#include <cstdio>
#include <vector>
#include <ctime>
#include <chrono>
#include <android/log.h>
#include <GLES3/gl3.h>
#include "Unity/IUnityGraphics.h"
#define STB_IMAGE_IMPLEMENTATION
#include "stb_image.h"

#define  LOG_TAG    "----------------- VREEL: CppPlugin - "
#define  LOGI(...)  __android_log_print(ANDROID_LOG_INFO, LOG_TAG, __VA_ARGS__)

// **************************
// Member Variables
// **************************

int m_numInits = 0; // Acts a bit like a reference counter, ensuring only 1 Init() and 1 Terminate()

GLuint* m_textureIDs;
int m_initMaxNumTextures = 0; // Set on Init - sets maximum textures to gen!
int m_currTextureIndex = 0;

stbi_uc* m_pCurrImage = NULL;
int m_maxImageWidth = 4096; // 2^12 to begin with - This is set at runtime in order to limit size of Gallery Images
int m_currImageWidth = 0;
int m_currImageHeight = 0;

const int kNumStbChannels = 3;
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

stbi_uc* ResampleIntegerRGB(stbi_uc *rgb_in, int w, int h, int stride, int new_w, int new_h, int new_stride)
{
    stbi_uc* result = (stbi_uc*) stbi__malloc((size_t) new_h * new_stride);
    int x_ratio = w / new_w;
    int y_ratio = h / new_h;
    int area_ratio = x_ratio * y_ratio;

    for (int y = 0; y != new_h; ++y)
    {
        for (int x = 0; x != new_w; ++x)
        {
            // take the average of the pixels in the NxM box
            int r = 0, g = 0, b = 0;
            for (int j = 0; j != x_ratio; ++j)
            {
                for (int i = 0; i != y_ratio; ++i)
                {
                    r += rgb_in[0 + (x*x_ratio+i)*3 + (y*y_ratio+j)*stride];
                    g += rgb_in[1 + (x*x_ratio+i)*3 + (y*y_ratio+j)*stride];
                    b += rgb_in[2 + (x*x_ratio+i)*3 + (y*y_ratio+j)*stride];
                }
            }

            result[0 + x*3 + y*new_stride] = stbi_uc(r / area_ratio);
            result[1 + x*3 + y*new_stride] = stbi_uc(g / area_ratio);
            result[2 + x*3 + y*new_stride] = stbi_uc(b / area_ratio);
        }
    }
    return result;
}

//CURRENTLY UNUSED
template<int x_ratio, int y_ratio>
stbi_uc* ResampleConstRGB(stbi_uc *rgb_in, int w, int h, int stride, int new_stride)
{
    int new_w = w / x_ratio;
    int new_h = h / y_ratio;
    stbi_uc* result = (stbi_uc*) stbi__malloc((size_t) new_h * new_stride);
    constexpr uint32_t area_ratio = (uint32_t) x_ratio * y_ratio;
    uint32_t row_len = (uint32_t) new_w * x_ratio * 3;
    std::vector<uint32_t> row(row_len);

    for (int y = 0; y != new_h; ++y)
    {
        for (int k = 0; k != row_len; ++k)
        {
            row[k] = rgb_in[k + (y*x_ratio+0)*stride];
        }

        for (int j = 1; j != y_ratio; ++j)
        {
            for (int k = 0; k != row_len; ++k)
            {
                row[k] += rgb_in[k + (y*x_ratio+j)*stride];
            }
        }

        for (int x = 0; x != new_w; ++x)
        {
            for (int comp = 0; comp != 3; ++comp)
            {
                uint32_t r = row[x * (x_ratio * 3) + comp];
                for (int i = 1; i != x_ratio; ++i)
                {
                    r += row[x * (x_ratio * 3) + i * 3 + comp];
                }
                result[comp + x*3 + y*new_stride] = stbi_uc(r / area_ratio);
            }
        }
    }
    return result;
}

bool DownsampleImageToMaxWidth()
{
    if (m_currImageWidth > m_maxImageWidth)
    {
        auto wcts = std::chrono::high_resolution_clock::now();

        int newWidth = m_currImageWidth;
        while (newWidth > m_maxImageWidth)
        {
            newWidth /= 2;
        }

        int newHeight = newWidth / 2;
        int newStride = (newWidth * 3 + 3) & ~3; // round up to multiple of four bytes
        stbi_uc *new_rgb =
                ResampleIntegerRGB(m_pCurrImage, m_currImageWidth, m_currImageHeight, m_currImageWidth * kNumStbChannels,
                                   newWidth, newHeight, newStride);

        memcpy(m_pCurrImage, new_rgb, newWidth * newHeight * kNumStbChannels);
        stbi_image_free(new_rgb);

        m_currImageWidth = newWidth;
        m_currImageHeight = newHeight;

        std::chrono::duration<double> wctduration = (std::chrono::high_resolution_clock::now() - wcts);
        LOGI("ResampleImageToMaxWidth() walltime = %f", wctduration.count());

        return true;
    }

    return false;
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
    kRenewTextureHandle = 3,
    kTerminate = 4
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

    LOGI("glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, %d, %d, 0, GL_RGB, GL_UNSIGNED_BYTE, NULL)", m_currImageWidth, m_currImageHeight);
    auto wcts = std::chrono::high_resolution_clock::now();
    glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, m_currImageWidth, m_currImageHeight, 0, GL_RGB, GL_UNSIGNED_BYTE, NULL); //(unsigned char*) m_pWorkingMemory);
    std::chrono::duration<double> wctduration = (std::chrono::high_resolution_clock::now() - wcts);

    LOGI("glTexImage2D() walltime = %f", wctduration.count());
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

    LOGI("glTexSubImage2D(GL_TEXTURE_2D, 0, 0, %d, %d, %d, GL_RGB, GL_UNSIGNED_BYTE, pImage", m_textureLoadingYOffset, m_currImageWidth, height);
    stbi_uc* pImage = m_pCurrImage + (m_textureLoadingYOffset * m_currImageWidth * kNumStbChannels);
    glTexSubImage2D(GL_TEXTURE_2D, 0, 0, m_textureLoadingYOffset, m_currImageWidth, height, GL_RGB, GL_UNSIGNED_BYTE, pImage);
    PrintAllGlError();

    m_textureLoadingYOffset += kIdealNumberOfScanlinesToUpload;
    if (m_textureLoadingYOffset > m_currImageHeight)
    {
        m_isLoadingIntoTexture = false;
        stbi_image_free(m_pCurrImage);

        LOGI("glGenerateMipmap(GL_TEXTURE_2D)");
        glGenerateMipmap(GL_TEXTURE_2D);
        PrintAllGlError();
    }

    LOGI("Finished LoadScanlinesIntoTextureFromWorkingMemory()! Loading in progress = %d", m_isLoadingIntoTexture);
}

// Trying this out to see if it improves performance...
void RenewTextureHandle()
{
    LOGI("Calling RenewTextureHandle()");

    LOGI("glDeleteTextures(1, %d)", m_currTextureIndex);
    auto wcts = std::chrono::high_resolution_clock::now();
    glDeleteTextures(1, m_textureIDs + m_currTextureIndex);
    std::chrono::duration<double> wctduration = (std::chrono::high_resolution_clock::now() - wcts);

    LOGI("glDeleteTextures() walltime = %f", wctduration.count());
    PrintAllGlError();

    LOGI("glGenTextures(1, %d)", m_currTextureIndex);
    glGenTextures(1, m_textureIDs + m_currTextureIndex);
    PrintAllGlError();
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
    else if (eventID == kRenewTextureHandle)
    {
        RenewTextureHandle();
    }
    else if (eventID == kTerminate)
    {
        Terminate();
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

void SetMaxImageWidth(int maxImageWidth)
{
    m_maxImageWidth = maxImageWidth;
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
    return (void*)(intptr_t)(m_textureIDs[m_currTextureIndex]);
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

    m_pCurrImage = stbi_load(pFileName, &m_currImageWidth, &m_currImageHeight, &type, kNumStbChannels);

    DownsampleImageToMaxWidth(); // Only necessary for images on phone, those loaded through LoadIntoWorkingMemoryFromImageData() are already at a max resolution...

    LOGI("Image Loaded has Width = %d, Height = %d, Type = %d\n", m_currImageWidth, m_currImageHeight, type);

    LOGI("Finished LoadIntoWorkingMemoryFromImagePath()!");

    return (m_currImageWidth * m_currImageHeight) > 0;
}

bool LoadIntoWorkingMemoryFromImageData(void* pRawData, int dataLength)
{
    LOGI("Calling LoadIntoWorkingMemoryFromImageData()");

    int type = -1;
    m_currImageWidth = m_currImageHeight = 0;

    m_pCurrImage = stbi_load_from_memory((stbi_uc*) pRawData, dataLength, &m_currImageWidth, &m_currImageHeight, &type, kNumStbChannels);

    // DownsampleImageToMaxWidth(); Unnecessary here...

    LOGI("Image Loaded has Width = %d, Height = %d, Type = %d\n", m_currImageWidth, m_currImageHeight, type);

    LOGI("Finished LoadIntoWorkingMemoryFromImageData()!");

    return (m_currImageWidth * m_currImageHeight) > 0;
}

jstring Java_com_soul_cppplugin_MainActivity_stringFromJNI(JNIEnv *env, jobject /* this */)
{
    std::string hello = "Hello from C++!";
    return env->NewStringUTF(hello.c_str());
}

}