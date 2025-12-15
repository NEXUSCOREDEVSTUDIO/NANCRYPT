#include "pch.h"
#include "NativeLib.h"
#include <fstream>
#include <string>
#include <vector>
#include <iostream>

// Simple XOR encryption for demonstration
// In a real antivirus/crypto app, you'd use OpenSSL or Windows Crypt API
void ProcessFile(const char* inputPath, const char* password, bool decrypt) {
    std::string path(inputPath);
    std::string pass(password);
    
    if (pass.empty()) return;

    std::ifstream inFile(path, std::ios::binary);
    if (!inFile) return;

    std::vector<char> buffer((std::istreambuf_iterator<char>(inFile)), std::istreambuf_iterator<char>());
    inFile.close();

    size_t passLen = pass.length();
    for (size_t i = 0; i < buffer.size(); ++i) {
        buffer[i] ^= pass[i % passLen];
    }

    std::ofstream outFile(path, std::ios::binary);
    outFile.write(buffer.data(), buffer.size());
    outFile.close();
}

NANCRYPT_API int EncryptFileNative(const char* inputPath, const char* password) {
    try {
        ProcessFile(inputPath, password, false);
        return 0; // Success
    } catch (...) {
        return 1; // Error
    }
}

NANCRYPT_API int DecryptFileNative(const char* inputPath, const char* password) {
    try {
        ProcessFile(inputPath, password, true); // XOR is symmetric
        return 0; // Success
    } catch (...) {
        return 1; // Error
    }
}

// "Signature" for our fake virus: EICAR-like string
const std::string VIRUS_SIGNATURE = "X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*";

NANCRYPT_API int ScanFileNative(const char* inputPath) {
    try {
        std::ifstream inFile(inputPath, std::ios::binary);
        if (!inFile) return -1; // File not found

        std::string content((std::istreambuf_iterator<char>(inFile)), std::istreambuf_iterator<char>());
        
        if (content.find(VIRUS_SIGNATURE) != std::string::npos) {
            return 1; // Virus Found
        }
        
        return 0; // Clean
    } catch (...) {
        return -2; // Error
    }
}
