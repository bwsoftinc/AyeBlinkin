#include "Fire.h"

Fire::Fire(uint8_t sparking, uint8_t cooling) :
  SPARKING(sparking),
  COOLING(cooling) {
}

void Fire::reset() {
  for(uint8_t i = 0; i < HEIGHT * 2; i++)
    heat[i] = 0;
}

void Fire::draw(CRGB* leds) {  
  uint8_t i;

  // cool
  for(i = 0; i < HEIGHT; i++)
    heat[i] = qsub8(heat[i],  random8(0, ((COOLING * 10) / HEIGHT) + 2));
    
  for(i = LED_COUNT - 1; i >= LED_COUNT - HEIGHT; i--)
    heat[i] = qsub8(heat[i],  random8(0, ((COOLING * 10) / HEIGHT) + 2));

  // heat rises
  for(i = HEIGHT - 1; i >= 2; i--)
    heat[i] = (heat[i - 1] + heat[i - 2] + heat[i - 2] ) / 3;

  for(i = LED_COUNT - HEIGHT-1; i <= LED_COUNT - 2; i++)
    heat[i] = (heat[i + 1] + heat[i + 2] + heat[i + 2] ) / 3;
  
  // new heat
  if(random8() < SPARKING ) {
    i = random8(2);
    heat[i] = qadd8(heat[i], random8(160,255));
  }

  if(random8() < SPARKING ) {
    i = random8(2);
    heat[LED_COUNT-1-i] = qadd8(heat[LED_COUNT-1-i], random8(160,255));
  }

  // draw
  for(i = 0; i < HEIGHT; i++)
    leds[i] = HeatColor(heat[i]);

  for(i = LED_COUNT - 1; i >= LED_COUNT - HEIGHT; i--)
    leds[i] = HeatColor(heat[i]);
  
  delay(50);
}
