#include "Pacifica.h"

Pacifica::Pacifica() {
}

void Pacifica::reset() {
  sCIStart1 = 0;
  sCIStart2 = 0;
  sCIStart3 = 0;
  sCIStart4 = 0;
  sLastms = 0;
}

void Pacifica::draw(CRGB* leds) {  
  // Increment the four "color index start" counters, one for each wave layer.
  // Each is incremented at a different speed, and the speeds vary over time.
  uint32_t ms = GET_MILLIS();
  uint8_t deltams = (uint8_t)(ms - sLastms);
  sLastms = ms;
  
  uint32_t deltams1 = (deltams * beatsin16(3, 179, 269)) / 256;
  uint32_t deltams2 = (deltams * beatsin16(4, 179, 269)) / 256;
  uint32_t deltams21 = (deltams1 + deltams2) / 2;
  sCIStart1 += (deltams1 * beatsin88(1011,10,13));
  sCIStart2 -= (deltams21 * beatsin88(777,8,11));
  sCIStart3 -= (deltams1 * beatsin88(501,5,7));
  sCIStart4 -= (deltams2 * beatsin88(257,4,6));

  // Clear out the LED array to a dim background blue-green
  fill_solid(leds, LED_COUNT, CRGB(2, 6, 10));

  // Render each of four layers, with different scales and speeds, that vary over time
  pacifica_one_layer(leds, pacifica_palette_1, sCIStart1, beatsin16(3, 11 * 256, 14 * 256), beatsin8(10, 70, 130), 0-beat16(301) );
  pacifica_one_layer(leds, pacifica_palette_2, sCIStart2, beatsin16(4,  6 * 256,  9 * 256), beatsin8(17, 40,  80), beat16(401) );
  pacifica_one_layer(leds, pacifica_palette_3, sCIStart3, 6 * 256, beatsin8(9, 10,38), 0-beat16(503));
  pacifica_one_layer(leds, pacifica_palette_3, sCIStart4, 5 * 256, beatsin8(8, 10,28), beat16(601));

  // Add brighter 'whitecaps' where the waves lines up more
  pacifica_add_whitecaps(leds);

  // Deepen the blues and greens a bit
  pacifica_deepen_colors(leds);
  
  delay(10);
}

// Add one layer of waves into the led array
void Pacifica::pacifica_one_layer(CRGB* leds, CRGBPalette16& p, uint16_t ci, uint16_t wavescale, uint8_t bri, uint16_t waveangle)
{
  uint8_t i;
  wavescale = (wavescale / 2) + 20;
  for(i = 0; i < LED_COUNT; i++) {
    waveangle += 250;
    ci += scale16(sin16(waveangle) + 32768, wavescale) + wavescale;
    leds[i] += ColorFromPalette(p, scale16(sin16(ci) + 32768, 240), bri, LINEARBLEND);;
  }
}

// Add extra 'white' to areas where the four layers of light have lined up brightly
void Pacifica::pacifica_add_whitecaps(CRGB* leds)
{
  uint8_t threshold, l, overage, overage2, i, wave = beat8(7);  
  for(i = 0; i < LED_COUNT; i++) {
    threshold = scale8(sin8(wave), 20) + beatsin8(9, 55, 65);
    wave += 7;
    l = leds[i].getAverageLight();
    if(l > threshold) {
      overage = l - threshold;
      overage2 = qadd8(overage, overage);
      leds[i] += CRGB(overage, overage2, qadd8(overage2, overage2));
    }
  }
}

// Deepen the blues and greens
void Pacifica::pacifica_deepen_colors(CRGB* leds)
{
  uint8_t i;
  for(i = 0; i < LED_COUNT; i++) {
    leds[i].blue = scale8(leds[i].blue, 145); 
    leds[i].green= scale8(leds[i].green, 200); 
    leds[i] |= CRGB(2, 5, 7);
  }
}
