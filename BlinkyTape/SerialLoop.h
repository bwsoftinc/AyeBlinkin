#include "BlinkyTape.h"

#define COMMAND_SIZE   0x03 // byte size of an incoming command
#define INTERRUPT      0xFF // byte that signals the start of a command
#define CMD_PATTERNS   0xFE // tx command to set pattern
#define CMD_BRIGHTNESS 0xFD // tx command to set brightness
#define CMD_PATTERN    0xFC // tx command to set pattern
#define CMD_CONTINUE   0xFB // tx command to continue transmission
#define BUILTIN_PATTERN_LIMIT 0xF0 // above this are streaming patterns

//command index 1 (index 0 is interrupt)
#define RED             0x00 // rx command to set red level
#define GREEN           0x01 // rx command to set green level
#define BLUE            0x02 // rx command to set blue level
#define BRIGHTNESS      0x03 // rx commadn to set brightness level
#define CMD_NUMBER_LEDS 0xFA // rx set the number of leds in string
#define CMD_INITIALIZE  0xFE // rx command to send the init info (loaded patterns, brightness, selected pattern)

//command index 2 (command 0 and 1 are interrupt)
#define CMD_RESET_INDEX 0xF0 // rx reset led index counter to 0

#define SERIAL_CR 0x0D // tx byte between data values

extern void serialLoop(CRGB* leds);
extern void sendPatterns();
extern void sendPattern();
extern void sendBrightness();
