// ESP32 DevKit
volatile unsigned long lastMs = 0;
volatile unsigned int count = 0;

const byte buttonPin = 4;

void onPress() {
  Serial.println("PRESSED!");
  if (millis() - lastMs > 200) {
    count++;
    lastMs = millis();
  }
}

void setup() {
  Serial.begin(115200);
  pinMode(buttonPin, INPUT_PULLUP);
  attachInterrupt(digitalPinToInterrupt(buttonPin), onPress, FALLING);
}

void loop() {
  noInterrupts();
  unsigned int safe = count;
  interrupts();
  
  Serial.print("Button pressed count: ");
  Serial.println(safe);
  delay(100);
}