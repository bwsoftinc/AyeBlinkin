#include "BlinkyTape.h"

#define COMMAND_SIZE 0x03   // byte size of an incoming command
#define INTERRUPT    0xFF   // byte that signals the start of a command
#define CMD_PATTERNS 0xFE   // tx command to set pattern
#define CMD_BRIGHTNESS 0xFD // tx command to set brightness
#define CMD_PATTERN  0xFC   // tx command to set pattern

#define RED 0x00        // rx command to set red level
#define GREEN 0x01      // rx command to set green level
#define BLUE 0x02       // rx command to set blue level
#define BRIGHTNESS 0x03 // rx commadn to set brightness level

#define SERIAL_CR 0x0D // tx byte between data values

extern void serialLoop(CRGB* leds);
extern void sendPatterns();
extern void sendPattern();
extern void sendBrightness();
