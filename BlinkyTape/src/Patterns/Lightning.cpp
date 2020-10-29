#include "Lightning.h"

Lightning::Lightning(uint8_t frequency, uint8_t flashes) :
  flashes(flashes),
  frequency(frequency) {
}

void Lightning::reset() {
  startIndex = random8(LED_COUNT);
  endIndex = random8(LED_COUNT);
  if(startIndex > endIndex) {
    flashCounter = startIndex;
    startIndex = endIndex;
    endIndex = flashCounter;
  }

  level = 255 - random8(0, 50);
  flashCounter = 0;
  flashLength = random8(4, flashes);
  flash = true;
  LEDS.clear();
}

void Lightning::draw(CRGB* leds) {  
  if(flash) { //draw one flash of the strike
    fill_solid(&leds[startIndex], endIndex - startIndex, CRGB(level, level, level));
    LEDS.show();

    delay(random8(4,10));
    LEDS.clear();
    
    delayCount = flashCounter == 0? random8(50, 100) : 0; //first flash extra delay

    if(++flashCounter == flashLength) // last flash, larger delay before next strike
      delayCount += random8(frequency) * 100;
    else
      delayCount += random8(10, 60);

    level -= 15; // dim each flash
    flash = false;
  } 
    
  delay(5);
  delayCount -= 5;
  if(delayCount <= 0) {
      flash = true;
      if(flashCounter == flashLength) // end of strike
        reset(); // generate params for next strike
  }
}
