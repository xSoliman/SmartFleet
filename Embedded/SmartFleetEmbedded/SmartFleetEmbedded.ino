// ESP8266 + SIM808 GPS to TCP Client
// Sends GPS data to ASP.NET application via TCP/GPRS

// Configuration
const char* SIM_CARD_NUMBER = "01505932589";  // Your SimCard number
const char* DEVICE_ID = "865067029129670";    // Device IMEI number
const char* DEVICE_MODEL = "SIM808";          // Device model
const char* SERVER_URL = "pbh67c409l.loclx.io"; // Your server URL

void setup() {
  delay(500); // Further reduced initialization time
  Serial.begin(115200);
  
  // Wait for SIM808 to initialize
  delay(1000); // Further reduced from 2000ms
  
  Serial.println("Initializing SIM808...");
  
  // Disable echo (keep this - important for clean communication)
  Serial.println("ATE0");
  delay(200); // Further reduced from 500ms
  readSIM808Response();

  // Test communication with SIM808 (keep this - essential)
  Serial.println("AT");
  delay(200); // Further reduced from 500ms
  readSIM808Response();

  // Turn on GPS power (keep this - essential)
  Serial.println("AT+CGPSPWR=1");
  delay(200); // Further reduced from 500ms
  readSIM808Response();

  // Reset GPS (important for reliable GPS initialization)
  Serial.println("AT+CGPSRST=0");
  delay(200); // Optimized delay
  readSIM808Response();

  // Wait for GPS to start (minimal wait time)
  Serial.println("Waiting for GPS to initialize...");
  delay(1000); // Further reduced from 2000ms
  
  // Setup GPRS connection
  setupGPRS();
}

void loop() {
  // Request GPS data
  Serial.println("AT+CGPSINF=32");
  delay(200); // Further reduced from 500ms
  String gpsData = readGPSData();
  
  // If we have valid GPS data, send it to the server
  if (gpsData.length() > 0) {
    // Parse the GPS data
    float latitude = 0.0;
    float longitude = 0.0;
    float speed = 0.0;
    parseGPS(gpsData, &latitude, &longitude, &speed);
    
    // Send the data to the server using SimCard number
    sendDataToServer(latitude, longitude, speed);
  }
  
  // Wait 5 seconds before next request (very fast updates for real-time tracking)
  delay(5000);
}

// Function to read GPS data from SIM808 and extract raw coordinates
String readGPSData() {
  String gpsInfo = "";
  unsigned long startTime = millis();
  
  while (millis() - startTime < 300) { // Further reduced from 500ms - GPS response is usually very fast
    if (Serial.available()) {
      char c = Serial.read();
      gpsInfo += c;
    }
  }
  
  // Look for GPS data in format: +CGPSINF: 32,001424.000,V,2711.2563,N,03110.6455,E,0.000,0.00,060180,,,N
  int infoIndex = gpsInfo.indexOf("+CGPSINF: 32,");
  if (infoIndex != -1) {
    // Return the raw GPS info for parsing
    return gpsInfo.substring(infoIndex);
  }
  
  return "";
}

// Parse GPS data into latitude, longitude, and speed
void parseGPS(String gpsData, float *latitude, float *longitude, float *speed) {
  // Parse the string by commas
  int infoIndex = gpsData.indexOf("+CGPSINF: 32,");
  if (infoIndex == -1) return;
  
  int commaCount = 0;
  int startPos = infoIndex + 13; // Start after "+CGPSINF: 32,"
  String utcTime = "";
  String status = "";
  String latStr = "";
  String latDir = "";
  String longStr = "";
  String longDir = "";
  String speedStr = "";
  
  for (int i = startPos; i < gpsData.length(); i++) {
    if (gpsData.charAt(i) == ',') {
      commaCount++;
      continue;
    }
    
    switch (commaCount) {
      case 0: // UTC time
        utcTime += gpsData.charAt(i);
        break;
      case 1: // Status (A=valid, V=invalid)
        status += gpsData.charAt(i);
        break;
      case 2: // Latitude
        latStr += gpsData.charAt(i);
        break;
      case 3: // Latitude direction (N/S)
        latDir += gpsData.charAt(i);
        break;
      case 4: // Longitude
        longStr += gpsData.charAt(i);
        break;
      case 5: // Longitude direction (E/W)
        longDir += gpsData.charAt(i);
        break;
      case 6: // Speed in knots
        speedStr += gpsData.charAt(i);
        break;
    }
    
    // Stop once we've got the speed
    if (commaCount > 6) break;
  }
  
  // Convert latitude from DDMM.MMMM format to decimal degrees
  if (latStr.length() > 0) {
    float lat = latStr.toFloat();
    int degrees = int(lat / 100);
    float minutes = lat - (degrees * 100);
    *latitude = degrees + (minutes / 60.0);
    
    // Adjust for direction
    if (latDir == "S") {
      *latitude = -*latitude;
    }
  }
  
  // Convert longitude from DDDMM.MMMM format to decimal degrees
  if (longStr.length() > 0) {
    float lng = longStr.toFloat();
    int degrees = int(lng / 100);
    float minutes = lng - (degrees * 100);
    *longitude = degrees + (minutes / 60.0);
    
    // Adjust for direction
    if (longDir == "W") {
      *longitude = -*longitude;
    }
  }
  
  // Set speed (convert from knots to km/h)
  if (speedStr.length() > 0) {
    *speed = speedStr.toFloat();
    // Convert knots to km/h
    *speed = *speed * 1.852;
  }
}

// Setup GPRS connection
void setupGPRS() {
  Serial.println("Setting up GPRS connection...");
  
  // Attach to GPRS service
  Serial.println("AT+CGATT=1");
  delay(500); // Further reduced from 1000ms
  readSIM808Response();

  // Set APN - Use "internet.we" for WE Egypt
  Serial.println("AT+CSTT=\"internet.we\",\"\",\"\"");
  delay(500); // Further reduced from 1000ms
  readSIM808Response();

  // Bring up wireless connection
  Serial.println("AT+CIICR");
  delay(800); // Further reduced from 1500ms
  readSIM808Response();

  // Note: Removed AT+CIFSR (IP address check) to save time - not essential for GPS tracking
}

// Function to send data to the ASP.NET server using SimCard number
void sendDataToServer(float latitude, float longitude, float speed) {
  // Format JSON payload with SimCard number instead of VehicleId
  String json = "{\"simCardNumber\":\"" + String(SIM_CARD_NUMBER) + "\"" +
                ",\"latitude\":" + String(latitude, 6) +
                ",\"longitude\":" + String(longitude, 6) +
                ",\"speed\":" + String(speed, 2) +
                ",\"deviceId\":\"" + String(DEVICE_ID) + "\"" +
                ",\"deviceModel\":\"" + String(DEVICE_MODEL) + "\"}";
  
  // Length of the data to send
  int dataLength = json.length();
  
  // Server details
  String server = SERVER_URL;
  String endpoint = "/api/Location/update";
  
  // Start TCP connection
  Serial.println("AT+CIPSTART=\"TCP\",\"" + server + "\",80");
  delay(800); // Further reduced from 1500ms
  readSIM808Response();

  // Calculate the total HTTP request length
  String httpHeader = "POST " + endpoint + " HTTP/1.1\r\n" +
                     "Host: " + server + "\r\n" +
                     "Content-Type: application/json\r\n" +
                     "Content-Length: " + String(dataLength) + "\r\n" +
                     "Connection: close\r\n\r\n";
  
  int totalLength = httpHeader.length() + json.length();
  
  // Prepare to send data with the correct total length
  Serial.println("AT+CIPSEND=" + String(totalLength));
  delay(200); // Further reduced from 500ms
  
  // Look for '>' prompt
  String response = "";
  unsigned long startTime = millis();
  bool promptFound = false;
  
  while (millis() - startTime < 2000) { // Further reduced from 3000ms
    if (Serial.available()) {
      char c = Serial.read();
      response += c;
      if (c == '>') {
        promptFound = true;
        break;
      }
    }
  }
  
  if (promptFound) {
    // Send the HTTP POST request with headers
    Serial.print(httpHeader);
    
    // Send the JSON body
    Serial.print(json);
    
    // Wait for response (minimal wait time)
    delay(1000); // Further reduced from 2000ms
    readSIM808Response();

  } else {
    Serial.println("Failed to get send prompt");
  }
  
  // Close the connection
  Serial.println("AT+CIPCLOSE");
  delay(200); // Further reduced from 500ms
  readSIM808Response();
}

// Function to read and print SIM808 responses
void readSIM808Response() {
  String response = "";
  unsigned long startTime = millis();
  
  while (millis() - startTime < 500) { // Further reduced from 1000ms - most responses are very quick
    if (Serial.available()) {
      char c = Serial.read();
      response += c;
      Serial.write(c); // Echo back for debugging
    }
  }
}

void readFullResponse() {
  String response = "";
  unsigned long start = millis();
  while (millis() - start < 1500) { // Further reduced from 3000ms
    while (Serial.available()) {
      char c = Serial.read();
      response += c;
      Serial.write(c);
    }
  }
  Serial.println("\n---- END OF RESPONSE ----");
}
