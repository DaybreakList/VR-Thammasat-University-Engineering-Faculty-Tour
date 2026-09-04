// ESP32 DevKit
const byte ledPin = 13;
const byte interruptPin = 4;
volatile byte state = LOW;

void setup() {
  Serial.begin(115200);
  pinMode(ledPin, OUTPUT);
  pinMode(interruptPin, INPUT_PULLUP);
  attachInterrupt(digitalPinToInterrupt(interruptPin), blink, CHANGE);
}

void loop() {
  digitalWrite(ledPin, state);
}

void blink() {
  Serial.print("PRESSED! : ");
  state = !state;
  Serial.println((state == 1) ? "LIT" : "DROP");
}