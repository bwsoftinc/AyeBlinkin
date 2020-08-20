#include "SerialLoop.h"

void serialLoop(CRGB* leds) {
  bool playPattern = true;
  uint8_t ledIndex = 0, command0, command1, command2;
  
  while(true) {
    while(Serial.available() < COMMAND_SIZE)
      continue;

    command0 = Serial.read();
    command1  = Serial.read();
    command2  = Serial.read();

    if(command0 != INTERRUPT) {
      leds[ledIndex++].setRGB(command0, command1, command2);
      if(ledIndex != DEFAULT_LED_COUNT)
        continue;
      else
        ledIndex = 0;

    } else if(command1 < BRIGHTNESS) {
      for(ledIndex = 0; ledIndex < DEFAULT_LED_COUNT; ledIndex++)
        leds[ledIndex][command1] = command2;

    } else if (command1 == BRIGHTNESS) {
      setBrightness(command2);
      if(playPattern)
        return;

    } else if (command1 != INTERRUPT) {
      sendPatterns();
      sendBrightness();
      sendPattern();
      return;

    } else if (command2 != INTERRUPT) {
      if(playPattern = command2 != CMD_PATTERNS) { //manual pattern (-2)
        setPattern(command2);
        return;
      }

    } else
      return;

    LEDS.show();
  }
}

void sendBrightness() {
  Serial.write(INTERRUPT);
  Serial.write(CMD_BRIGHTNESS);
  Serial.write(currentBrightness);
  Serial.write(INTERRUPT);
  Serial.flush();
}

void sendPattern() {
  Serial.write(INTERRUPT);
  Serial.write(CMD_PATTERN);
  Serial.write(currentPattern);
  Serial.write(INTERRUPT);
  Serial.flush();
}

void sendPatterns() {
  uint8_t i = 0;
  Serial.write(INTERRUPT);
  Serial.write(CMD_PATTERNS);
  for(;i<patternCount;i++) {
    Serial.write(patternNames[i]);
    Serial.write(SERIAL_CR);
  }
  Serial.write(INTERRUPT);
  Serial.flush();
}        