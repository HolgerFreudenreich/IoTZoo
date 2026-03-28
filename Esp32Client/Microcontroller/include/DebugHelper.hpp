#pragma once
#include "Defines.hpp"

#ifdef USE_DEBUG_MESSAGES
#include <TinyConsole.h>

struct DebugHelper
{
    static const int debugLevel = 2;
}; 

#define debug(what)                                                                                                                                  \
    {                                                                                                                                                \
        if (DebugHelper::debugLevel >= 1)                                                                                                                    \
            Console << "L" << (int)__LINE__ << ' ' << what << _EndLineCode::endl;                                                                           \
    }
#else
#define debug(what)                                                                                                                                  \
    {                                                                                                                                                \
    }
#endif // USE_DEBUG_MESSAGES