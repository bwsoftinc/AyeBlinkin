#include "BlinkyTape.h"
#include "SerialLoop.h"
#include "src/Patterns/ColorLoop.h"
#include "src/Patterns/Shimmer.h"
#include "src/Patterns/Scanner.h"
#include "src/Patterns/Flashlight.h"

// LED data array
struct CRGB leds[MAX_LEDS];   // Space to hold the LED data
CLEDController* controller;   // LED controller
Pattern* patterns[MAX_PATTERN_COUNT];

// Button interrupt counters
volatile bool buttonUnhandled = false;
volatile long buttonDownTime = 0;

// Brightness controls
volatile uint8_t currentBrightness;
volatile uint8_t currentBrightnessStep;

// Pattern controls
uint8_t patternCount = 0;
uint8_t currentPattern = 0;
char* patternNames[MAX_PATTERN_COUNT];

// Patterns
ColorLoop originalRainbow(1,1,1);
ColorLoop blueRainbow(.2,1,1);
Scanner scanner(4, CRGB(255,0,0));
Flashlight flashlight(CRGB(255,255,255));
Flashlight offlight(CRGB(0,0,0));
Shimmer shimmer(1,1,1);

// Register a pattern
void loadPattern(Pattern* newPattern, char* name) {
  if(patternCount >= MAX_PATTERN_COUNT) return;
  patterns[patternCount] = newPattern;
  patternNames[patternCount] = name;
  patternCount++;
}

// Change the current pattern
void setPattern(uint8_t newPattern) {
  currentPattern = newPattern % patternCount;
  
  if(EEPROM.read(PATTERN_EEPROM_ADDRESS) != currentPattern)
    EEPROM.write(PATTERN_EEPROM_ADDRESS, currentPattern);

  patterns[currentPattern]->reset();  
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
    setPattern(currentPattern+1);
    sendPattern();
  }
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

  loadPattern(&blueRainbow, "Blue Rainbow");
  loadPattern(&flashlight, "Flashlight");
  loadPattern(&scanner, "Knight Rider");
  loadPattern(&offlight, "Off");
  loadPattern(&originalRainbow, "Original Rainbow");
  loadPattern(&shimmer, "Shimmer");

  // Read in the last-used pattern and brightness
  setPattern(EEPROM.read(PATTERN_EEPROM_ADDRESS));
  setBrightness(EEPROM.read(BRIGHTNESS_EEPROM_ADDRESS));
  controller = &(LEDS.addLeds<WS2811, LED_OUT, GRB>(leds, DEFAULT_LED_COUNT));
  controller->setCorrection(TypicalLEDStrip);
  LEDS.show();
}

void loop() {
  if(Serial.available() > 0) {    
    serialLoop(leds);
    return;
  }
  
  // Draw the current pattern
  patterns[currentPattern]->draw(leds);
  LEDS.show();
}