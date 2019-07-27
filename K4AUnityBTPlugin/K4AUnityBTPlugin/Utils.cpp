#include "pch.h"
#include "Utils.h"

string Utils::WStringToString(wstring wideString)
{
	int bufferSize = WideCharToMultiByte(CP_OEMCP, 0, wideString.c_str(), -1, (char*)NULL, 0, NULL, NULL);
	CHAR* multiByteString = new CHAR[bufferSize];

	WideCharToMultiByte(CP_OEMCP, 0, wideString.c_str(), -1, multiByteString, bufferSize, NULL, NULL);

	string result(multiByteString, multiByteString + bufferSize - 1);
	delete[] multiByteString;
	return(result);
}

wstring Utils::StringToWString(string string)
{
	auto slength = (int)string.length() + 1;
	auto wlength = MultiByteToWideChar(CP_ACP, 0, string.c_str(), slength, 0, 0);
	wchar_t* wideString = new wchar_t[wlength];
	MultiByteToWideChar(CP_ACP, 0, string.c_str(), slength, wideString, wlength);
	wstring result(wideString);
	delete[] wideString;
	return result;
}

