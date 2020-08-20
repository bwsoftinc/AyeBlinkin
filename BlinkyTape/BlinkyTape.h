#ifndef BLINKY_TAPE_H
#define BLINKY_TAPE_H

#include <EEPROM.h>
#include <Arduino.h>
#include <FastLED.h>

// hardware defs
#define LED_OUT       13
#define BUTTON_IN     10
#define ANALOG_INPUT  A9
#define EXTRA_PIN_A    7
#define EXTRA_PIN_B   11
#define EEPROM_START_ADDRESS  0x00 // EEPROM offset for this program's use
#define EEPROM_MAGIG_BYTE_0   0x12 // just some randoms to know whether eeprom needs init
#define EEPROM_MAGIC_BYTE_1   0x34 // 
#define PATTERN_EEPROM_ADDRESS EEPROM_START_ADDRESS + 0x02
#define BRIGHTNESS_EEPROM_ADDRESS EEPROM_START_ADDRESS + 0x03

#define BUTTON_PATTERN_SWITCH_TIME 400  // ms to hold the button down to switch patterns
#define MAX_PATTERN_COUNT 0x0A          // how many patterns can be held in memory at runtime

const uint8_t DEFAULT_LED_COUNT = 20;  // Number of LEDs to display the patterns on
const uint8_t MAX_LEDS = 60;           // Maximum number of LEDs that can be controlled

#define BRIGHTNESS_STEP_COUNT 8        // the number of brightness steps to cycle through with button
const uint8_t brightnessSteps[BRIGHTNESS_STEP_COUNT] = {93, 70, 40, 15, 7, 15, 40, 70}; // the brightness steps to cycle through

extern void setPattern(uint8_t);
extern void setBrightness(uint8_t);
extern volatile uint8_t currentBrightness;
extern uint8_t currentPattern;
extern uint8_t patternCount;
extern char* patternNames[];

class Pattern {
  public:
    virtual void draw(CRGB * leds);
    virtual void reset();
};

#endif