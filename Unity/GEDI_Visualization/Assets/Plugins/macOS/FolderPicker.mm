#import <Cocoa/Cocoa.h>
#import <string.h>
#import <stdlib.h>

extern "C"
{
    const char* PickFolderNative(const char* title)
    {
        @autoreleasepool
        {
            NSOpenPanel* panel = [NSOpenPanel openPanel];
            [panel setCanChooseFiles:NO];
            [panel setCanChooseDirectories:YES];
            [panel setAllowsMultipleSelection:NO];
            [panel setCanCreateDirectories:NO];

            if (title != NULL)
            {
                NSString* nsTitle = [NSString stringWithUTF8String:title];
                [panel setTitle:nsTitle];
            }

            NSInteger result = [panel runModal];

            if (result == NSModalResponseOK)
            {
                NSURL* url = [[panel URLs] firstObject];
                if (url == nil)
                    return NULL;

                const char* utf8Path = [[url path] UTF8String];
                if (utf8Path == NULL)
                    return NULL;

                char* out = (char*)malloc(strlen(utf8Path) + 1);
                strcpy(out, utf8Path);
                return out;
            }

            return NULL;
        }
    }
}