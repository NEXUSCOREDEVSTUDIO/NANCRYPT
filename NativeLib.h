#pragma once

#ifdef NANCRYPTCORE_EXPORTS
#define NANCRYPT_API __declspec(dllexport)
#else
#define NANCRYPT_API __declspec(dllimport)
#endif

extern "C" {
    NANCRYPT_API int EncryptFileNative(const char* inputPath, const char* password);
    NANCRYPT_API int DecryptFileNative(const char* inputPath, const char* password);
    NANCRYPT_API int ScanFileNative(const char* inputPath);
}
