#include <jni.h>
#include <string>
#include <cstdio>
#include <vector>
#include <ctime>
#include <chrono>
#include <fstream>
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
bool m_useExif = false; // Only relates to files that live on the phone, not to files in the cloud
int m_maxImageWidth = 4096; // 2^12 to begin with - This is set at runtime in order to limit size of Gallery Images
int m_currImageWidth = 0;
int m_currImageHeight = 0;

const int kNumStbChannels = 3;
const int kStrideRGB565 = 2;
bool m_rgb565On = false;
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

// Trying this out to see if it improves performance... SEEMS USELESS FROM "wctduration.count()"
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

stbi_uc* ResampleIntegerRGB565(stbi_uc *rgb_in, int w, int h, int stride, int new_w, int new_h, int new_stride)
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
                    r += rgb_in[0 + (x * x_ratio + i) * 3 + (y * y_ratio + j) * stride];
                    g += rgb_in[1 + (x * x_ratio + i) * 3 + (y * y_ratio + j) * stride];
                    b += rgb_in[2 + (x * x_ratio + i) * 3 + (y * y_ratio + j) * stride];
                }
            }
            stbi_uc r8 = stbi_uc(r / area_ratio);
            stbi_uc g8 = stbi_uc(g / area_ratio);
            stbi_uc b8 = stbi_uc(b / area_ratio);
            (uint16_t &) result[x * 2 + y * new_stride] = (uint16_t) (
                    ((r8 >> 3) << 11) |
                    ((g8 >> 2) << 5) |
                    ((b8 >> 3) << 0)
            );
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

bool ReampleImageToMaxWidthAndNewType()
{
    auto wcts = std::chrono::high_resolution_clock::now();

    int newWidth = m_currImageWidth;
    while (newWidth > m_maxImageWidth)
    {
        newWidth /= 2;
    }

    int newHeight = newWidth / 2; // because of 2:1 ratio for 360-images
    int newStride = newWidth;
    stbi_uc* new_rgb = nullptr;

    if (m_rgb565On)
    {
        newStride = newWidth * kStrideRGB565;
        new_rgb = ResampleIntegerRGB565(m_pCurrImage, m_currImageWidth, m_currImageHeight, m_currImageWidth * kNumStbChannels,
                                        newWidth, newHeight, newStride);
    }
    else
    {
        newStride = newWidth * kNumStbChannels;
        new_rgb = ResampleIntegerRGB(m_pCurrImage, m_currImageWidth, m_currImageHeight, m_currImageWidth * kNumStbChannels,
                                        newWidth, newHeight, newStride);
    }

    memcpy(m_pCurrImage, new_rgb, (size_t) newHeight * newStride);
    stbi_image_free(new_rgb);

    m_currImageWidth = newWidth;
    m_currImageHeight = newHeight;

    std::chrono::duration<double> wctduration = (std::chrono::high_resolution_clock::now() - wcts);
    LOGI("ReampleImageToMaxWidthAndNewType() walltime = %f", wctduration.count());

    return true;
}

// return a vector containing JPEG thummnail for an EXif file.
std::vector<char> FindExifJpeg(const char* pFileName)
{
    std::ifstream f(pFileName, std::ios::binary);

    // Loop over JPEG markers
    while (!f.eof())
    {
        int t = (uint8_t)f.get();
        //printf("%08x %02x\n", (int)f.tellg()-1, t);
        if (t != 0xff)
        {
            break;
        }
        int marker = (uint8_t)f.get();

        if (marker == 0xd8 || marker == 0xd9)
        {
            // Start/end of image
            //printf("%08x %02x %02x size = %04x\n", (int)f.tellg(), t, marker, 0);
        }
        else if (marker == 0xda)
        {
            // start of JPEG data
            //printf("%08x %02x %02x size = %04x\n", (int)f.tellg(), t, marker, 0);
            break;
        }
        else
        {
            // General tag: size is encoded in two bytes.
            int hibyte = (uint8_t)f.get();
            int lobyte = (uint8_t)f.get();
            int size = hibyte * 0x100 + lobyte;
            //LOGI("%08x %02x %02x size = %04d\n", (int)f.tellg(), t, marker, size);

            // ff e1 tag is APP1
            if (marker == 0xe1)
            {
                std::vector<char> exif(size-2);
                f.read(exif.data(), size-2);
                char *d = exif.data();
                char *base = d + 6;
                // APP1 tags with "Exif\0\0" are Exif data encoded as TIFF data.
                static const char exifHdrLe[] = { 0x45, 0x78, 0x69, 0x66, 0x00, 0x00, 0x49, 0x49, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00 };
                static const char exifHdrBe[] = { 0x45, 0x78, 0x69, 0x66, 0x00, 0x00, 0x4D, 0x4D, 0x00, 0x2A, 0x00, 0x00, 0x00, 0x08 };
                auto sz = sizeof(exifHdrLe);
                bool isLe = exif.size() > sz + 2 && std::mismatch(d, d+sz, exifHdrLe).first == d+sz;
                bool isBe = exif.size() > sz + 2 && std::mismatch(d, d+sz, exifHdrBe).first == d+sz;
                char *dmax = d + exif.size();

                // Two byte orders are possible (madness!)
                if (isLe || isBe)
                {
                    //printf("EXIF!\n");
                    d += sz;
                    auto b2 = [&]()
                    {
                        int b0 = (uint8_t)*d++;
                        int b1 = (uint8_t)*d++;
                        return isBe ? b0 * 0x100 + b1 : b1 * 0x100 + b0;
                    };
                    auto b4 = [&]()
                    {
                        int w0 = b2();
                        int w1 = b2();
                        return isBe ? w0 * 0x10000 + w1 : w1 * 0x10000 + w0;
                    };

                    // Loop over TIFF tags finding the JPEG image data
                    int jpegOffset  = 0;
                    int jpegSize = 0;
                    while (d+2 <= dmax)
                    {
                        int numEntries = b2();
                        //printf("ne=%d\n", numEntries);
                        if (d + numEntries * 12 + 4 > dmax)
                        {
                            break;
                        }
                        for (int i = 0; i != numEntries; ++i)
                        {
                            int tag = b2();
                            int fmt = b2();
                            int nc = b4();
                            int off = b4();
                            //printf("%04x %04x %08x %08x\n", tag, fmt, nc, off);
                            if (tag == 0x0201) jpegOffset = off;
                            if (tag == 0x0202) jpegSize = off;
                        }
                        int next = b4();
                        //printf("%08x\n", next);
                        if (next)
                        {
                            d = base + next;
                        }
                        else
                            break;
                    }
                    //printf("%08x..%08x\n", jpegOffset, jpegSize);
                    if (jpegOffset && jpegSize && jpegOffset > 0 && base + jpegOffset <= dmax)
                    {
                        //printf("jpeg found\n");
                        char *b = base + jpegOffset;
                        char *e = b + jpegSize;
                        return std::vector<char>(b, e);
                    }
                }
            }
            else
            {
                f.seekg(size-2, std::ios::cur);
            }
        }
    }

    return std::vector<char>{};
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

    auto wcts = std::chrono::high_resolution_clock::now();

    if (m_rgb565On)
    {
        LOGI("glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, %d, %d, 0, GL_RGB, GL_UNSIGNED_SHORT_5_6_5, NULL)", m_currImageWidth, m_currImageHeight);
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, m_currImageWidth, m_currImageHeight, 0, GL_RGB, GL_UNSIGNED_SHORT_5_6_5, NULL);
    }
    else
    {
        LOGI("glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, %d, %d, 0, GL_RGB, GL_UNSIGNED_BYTE, NULL)", m_currImageWidth, m_currImageHeight);
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, m_currImageWidth, m_currImageHeight, 0, GL_RGB, GL_UNSIGNED_BYTE, NULL);
    }

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

    stbi_uc* pImage = m_pCurrImage;

    if (m_rgb565On)
    {
        LOGI("glTexSubImage2D(GL_TEXTURE_2D, 0, 0, %d, %d, %d, GL_RGB, GL_UNSIGNED_SHORT_5_6_5, pImage)", m_textureLoadingYOffset, m_currImageWidth, height);
        pImage = m_pCurrImage + (m_textureLoadingYOffset * m_currImageWidth * kStrideRGB565);
        glTexSubImage2D(GL_TEXTURE_2D, 0, 0, m_textureLoadingYOffset, m_currImageWidth, height, GL_RGB, GL_UNSIGNED_SHORT_5_6_5, pImage);
    }
    else
    {
        LOGI("glTexSubImage2D(GL_TEXTURE_2D, 0, 0, %d, %d, %d, GL_RGB, GL_UNSIGNED_BYTE, pImage)", m_textureLoadingYOffset, m_currImageWidth, height);
        pImage = m_pCurrImage + (m_textureLoadingYOffset * m_currImageWidth * kNumStbChannels);
        glTexSubImage2D(GL_TEXTURE_2D, 0, 0, m_textureLoadingYOffset, m_currImageWidth, height, GL_RGB, GL_UNSIGNED_BYTE, pImage);
    }

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

void SetUseExif(bool useExif)
{
    m_useExif = useExif;
}

void SetMaxImageWidth(int maxImageWidth)
{
    m_maxImageWidth = maxImageWidth;
}

void SetRGB565On(int rgb565On)
{
    m_rgb565On = rgb565On;
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

    int comp = -1;
    m_currImageWidth = m_currImageHeight = 0;

    if (m_useExif)
    {
        auto jpeg = FindExifJpeg(pFileName);
        std::ofstream("tempExif.jpg", std::ios::binary).write(jpeg.data(), jpeg.size());
        pFileName = (char*) "tempExif.jpg";
        //m_pCurrImage = reinterpret_cast<stbi_uc*>(jpeg.data());
    }

    m_pCurrImage = stbi_load(pFileName, &m_currImageWidth, &m_currImageHeight, &comp, kNumStbChannels);
    if (!m_useExif)
    {
        ReampleImageToMaxWidthAndNewType();
    }

    LOGI("Image Loaded has Width = %d, Height = %d, Comp = %d\n", m_currImageWidth, m_currImageHeight, comp);

    LOGI("Finished LoadIntoWorkingMemoryFromImagePath()!");

    return (m_currImageWidth * m_currImageHeight) > 0;
}

bool LoadIntoWorkingMemoryFromImageData(void* pRawData, int dataLength)
{
    LOGI("Calling LoadIntoWorkingMemoryFromImageData()");

    int comp = -1;
    m_currImageWidth = m_currImageHeight = 0;

    m_pCurrImage = stbi_load_from_memory((stbi_uc*) pRawData, dataLength, &m_currImageWidth, &m_currImageHeight, &comp, kNumStbChannels);

    if (m_rgb565On) // No need to resample images coming off the cloud if we are not updating them to 565, because they are uploaded at the correct width
    {
        ReampleImageToMaxWidthAndNewType();
    }

    LOGI("Image Loaded has Width = %d, Height = %d, Comp = %d\n", m_currImageWidth, m_currImageHeight, comp);

    LOGI("Finished LoadIntoWorkingMemoryFromImageData()!");

    return (m_currImageWidth * m_currImageHeight) > 0;
}

jstring Java_com_soul_cppplugin_MainActivity_stringFromJNI(JNIEnv *env, jobject /* this */)
{
    std::string hello = "Hello from C++!";
    return env->NewStringUTF(hello.c_str());
}

}