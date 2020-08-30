#ifndef BLINKY_TAPE_H
#define BLINKY_TAPE_H

#include <EEPROM.h>
#include <avr/pgmspace.h>
#include <Arduino.h>
#include <FastLED.h>

#define patternStr(X, Y) strcpy_P(X, (char*)pgm_read_word(&(patternNames[Y])));

// hardware defs
#define LED_OUT       13
#define BUTTON_IN     10
#define ANALOG_INPUT  A9
#define EXTRA_PIN_A    7
#define EXTRA_PIN_B   11

#define EEPROM_START_ADDRESS  0x00      // EEPROM offset for this program's use
#define EEPROM_MAGIG_BYTE_0   0x12      // random value to check if eeprom needs init
#define EEPROM_MAGIC_BYTE_1   0x34      // random value to check if eeprom needs init
#define PATTERN_EEPROM_ADDRESS 0x02     // EEPROM storage address for selected pattern
#define BRIGHTNESS_EEPROM_ADDRESS  0x03 // EEPROM storage address for selected brightness

#define BUTTON_PATTERN_SWITCH_TIME 400  // ms to hold the button down to switch patterns
#define MAX_PATTERN_COUNT 10            // how many patterns can be held in memory at runtime

//const uint8_t DEFAULT_LED_COUNT = 45;  // Number of LEDs to display the patterns on
const uint8_t MAX_LEDS = 60;            // Maximum number of LEDs that can be controlled

#define BRIGHTNESS_STEP_COUNT 8         // the number of brightness steps to cycle through with button
const uint8_t brightnessSteps[BRIGHTNESS_STEP_COUNT] 
  = {7, 15, 40, 70, 93, 115, 140, 180}; // the brightness steps to cycle through with button

extern void setNumberLeds(uint8_t);
extern void setPattern(uint8_t);
extern void setBrightness(uint8_t);

extern volatile uint8_t LED_COUNT;
extern volatile uint8_t currentBrightness;
extern volatile bool interruptSerialLoop;
extern uint8_t currentPattern;
extern uint8_t patternCount;
extern const char* const patternNames[];

class Pattern {
  public:
    virtual void draw(CRGB * leds);
    virtual void reset();
};

#endif
