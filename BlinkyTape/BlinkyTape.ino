#include "BlinkyTape.h"
#include "SerialLoop.h"
#include "src/Patterns/ColorLoop.h"
#include "src/Patterns/Shimmer.h"
#include "src/Patterns/Scanner.h"
#include "src/Patterns/Fire.h"
#include "src/Patterns/Flashlight.h"
#include "src/Patterns/Rainbow.h"
#include "src/Patterns/Lightning.h"
#include "src/Patterns/Plasma.h"

// LED data array
volatile uint8_t LED_COUNT = 45;
struct CRGB leds[MAX_LEDS];   // Space to hold the LED data
CLEDController* controller;   // LED controller
Pattern* patterns[MAX_PATTERN_COUNT];

// Button interrupt counters
volatile bool buttonUnhandled = false;
volatile unsigned long buttonDownTime = 0;
volatile bool interruptSerialLoop = false;

// Brightness controls
volatile uint8_t currentBrightness;
volatile uint8_t currentBrightnessStep;

// Pattern controls
uint8_t patternCount = 0;
uint8_t currentPattern = 0;

// Patterns
ColorLoop blueRainbow(51,255,255);
Scanner scanner(4, CRGB(255,0,0));
ColorLoop originalRainbow(255,255,255);
Flashlight off(CRGB(0,0,0));
Shimmer shimmer(255,255,255);
Fire fire(100,55);
Rainbow rainbowShift(0,85,170);
Lightning lightning(40,9);
Plasma plasma;

const char blueRainbowName[]      PROGMEM = "Blue Rainbow Run";
const char scannerName[]          PROGMEM = "Knight Rider";
const char offlightName[]         PROGMEM = "Off";
const char originalRainbowName[]  PROGMEM = "Rainbow Run";
const char shimmerName[]          PROGMEM = "Shimmer";
const char fireName[]             PROGMEM = "Fire";
const char rainbowShiftName[]     PROGMEM = "Rainbow Shift";
const char lightningName[]        PROGMEM = "Lightning";
const char plasmaName[]           PROGMEM = "Plasma";
const char* const patternNames[]  PROGMEM = { //keep this order the same as patterns are loaded
  blueRainbowName,
  scannerName,
  offlightName,
  originalRainbowName,
  shimmerName,
  fireName,
  rainbowShiftName,
  lightningName,
  plasmaName
};

// Register a pattern
void loadPattern(Pattern* newPattern) {
  if(patternCount >= MAX_PATTERN_COUNT) 
    return;
  
  patterns[patternCount] = newPattern;
  patternCount++;
}

// Change the current pattern
void setPattern(uint8_t newPattern) {
  currentPattern = newPattern % patternCount;
  
  if(EEPROM.read(PATTERN_EEPROM_ADDRESS) != currentPattern)
    EEPROM.write(PATTERN_EEPROM_ADDRESS, currentPattern);

  patterns[currentPattern]->reset();
  LEDS.clear();
}

// Change the current brightness
void setBrightness(uint8_t newBrightness) {
  currentBrightness = newBrightness;
  
  if(EEPROM.read(BRIGHTNESS_EEPROM_ADDRESS) != currentBrightness)
    EEPROM.write(BRIGHTNESS_EEPROM_ADDRESS, currentBrightness);

  LEDS.setBrightness(currentBrightness); 
}

// Called when the button is both pressed and released.
// Reading state of the PB6 (remember that HIGH == released)
ISR(PCINT0_vect){
  if(PINB & _BV(PINB6)) { // button up
    if(buttonUnhandled) {
      buttonUnhandled = false;      
      currentBrightnessStep = (currentBrightnessStep + 1) % BRIGHTNESS_STEP_COUNT;
      setBrightness(brightnessSteps[currentBrightnessStep]);
      sendBrightness();
    }
    TIMSK4 = 0;  // turn off the interrupt
  } else { // button down
    buttonUnhandled = true;
    buttonDownTime = millis();
    
    // And configure and start timer4 interrupt.
    TCCR4B = 0x0F;                    // Slowest prescaler
    TCCR4D = _BV(WGM41) | _BV(WGM40); // Fast PWM mode
    OCR4C  = 0x10;                    // some random percentage of the clock
    TCNT4  = 0;                       // Reset the counter
    TIMSK4 = _BV(TOV4);               // turn on the interrupt
  }
}

// This is called every xx ms while the button is being held down; it counts down then displays a
ISR(TIMER4_OVF_vect) {
  if(buttonUnhandled && (millis() - buttonDownTime) > BUTTON_PATTERN_SWITCH_TIME) {
    buttonUnhandled = false;
    interruptSerialLoop = true;
    setPattern(currentPattern+1);
    sendPattern();
  }
}

void setNumberLeds(uint8_t number) {
  LED_COUNT = number % MAX_LEDS;
  controller->setLeds(leds, LED_COUNT);
}

void setup(){
  Serial.begin(115200);

  pinMode(BUTTON_IN, INPUT_PULLUP);
  pinMode(ANALOG_INPUT, INPUT_PULLUP);
  pinMode(EXTRA_PIN_A, INPUT_PULLUP);
  pinMode(EXTRA_PIN_B, INPUT_PULLUP);
  
  // Interrupt set-up; see Atmega32u4 datasheet section 11
  PCIFR  |= _BV(PCIF0);  // Just in case, clear interrupt flag
  PCMSK0 |= _BV(PCINT6); // Set interrupt mask to the button pin (PCINT6)
  PCICR  |= _BV(PCIE0);  // Enable interrupt
  
  // If the EEPROM hasn't been initialized, do so now
  if((EEPROM.read(EEPROM_START_ADDRESS) != EEPROM_MAGIG_BYTE_0)
     || (EEPROM.read(EEPROM_START_ADDRESS + 1) != EEPROM_MAGIC_BYTE_1)) {
    EEPROM.write(EEPROM_START_ADDRESS, EEPROM_MAGIG_BYTE_0);
    EEPROM.write(EEPROM_START_ADDRESS + 1, EEPROM_MAGIC_BYTE_1);
    EEPROM.write(PATTERN_EEPROM_ADDRESS, 0);
    EEPROM.write(BRIGHTNESS_EEPROM_ADDRESS, 0);
  }

  loadPattern(&blueRainbow);
  loadPattern(&scanner);
  loadPattern(&off);
  loadPattern(&originalRainbow);
  loadPattern(&shimmer);
  loadPattern(&fire);
  loadPattern(&rainbowShift);
  loadPattern(&lightning);
  loadPattern(&plasma);
  
  // Read in the last-used pattern and brightness
  setPattern(EEPROM.read(PATTERN_EEPROM_ADDRESS));
  setBrightness(EEPROM.read(BRIGHTNESS_EEPROM_ADDRESS));

  controller = &LEDS.addLeds<WS2812B, LED_OUT, GRB>(leds, LED_COUNT);
  LEDS.setCorrection(0xFFE4BA);
  LEDS.show();
}

void loop() {
  if(!interruptSerialLoop && Serial.available() > 0) {    
    serialLoop(leds);
    return;
  }
  
  // Draw the current pattern
  patterns[currentPattern]->draw(leds);
  LEDS.show();
  interruptSerialLoop = false;
}