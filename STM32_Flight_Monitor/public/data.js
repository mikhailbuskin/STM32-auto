const ws = new WebSocket("ws://localhost:6060");


ws.onmessage = async (event) => {
  // event.data is blob
  var buff = await event.data.arrayBuffer();

  let tel = parseTelemetryData(buff);
  if (tel) {
    // send document event to update the UI
    document.dispatchEvent(new CustomEvent('telemetry', { detail: tel }));

    // show mode 
    // battery voltage
    // temperature
    // roll angle
    // pitch angle
    // compass heading
    // satellites count
    // altitude
    // ground distance

    var text = `Flight mode: ${tel.flight_mode}
    Battery voltage: ${tel.battery_voltage} V
    Temperature: ${tel.temperature} °C
    Roll angle: ${tel.roll_angle} °
    Pitch angle: ${tel.pitch_angle} °
    Compass heading: ${tel.actual_compass_heading} °
    Satellites count: ${tel.number_used_sats}
    Altitude: ${tel.altitude_meters} m
    Start: ${tel.start}
    `

    //document.getElementById('app').innerText = JSON.stringify(tel, null, 2);
    document.getElementById('app').innerText = text;
  }
};

ws.onopen = () => {
  console.log("WebSocket connected.");
};

ws.onclose = () => {
  console.log("WebSocket disconnected.");
};

let buffer = [];
function parseTelemetryData(data) {
    const telemetryPacketSize = 32; // Number of bytes in one telemetry packet

    // Add incoming data to the buffer
    // convers ArrayBuffer to array of bytes
    data = new Uint8Array(data);
    buffer.push(...data);

    // look for the start of the telemetry packet
    // and if found whole packet is extracted
    var packet = null;
    for (let i = 0; i < buffer.length-1; i++) {
      if (String.fromCharCode(buffer[i]) == 'J' && String.fromCharCode(buffer[i + 1]) == 'B' && buffer.length - i >= telemetryPacketSize) {
          // Found start of telemetry packet
          buffer = buffer.slice(i+2); // Discard any bytes before the start signature
          packet = buffer.splice(0, telemetryPacketSize); // Extract one packet
          break;
      }
    }

    // if packet found
    if (packet) {
      let check_byte = 0;
      let first_receive = 0;
      let last_receive = 0;
      let receive_start_detect = 0;
      let max_altitude_meters = -Infinity;
      let home_lat_gps = 0; // Replace with actual value
      let home_lon_gps = 0; // Replace with actual value
  
      // Calculate check byte
      for (let temp_byte = 0; temp_byte <= 30; temp_byte++) {
          check_byte ^= packet[temp_byte];
      }
  
      // Check if check byte matches
      if (check_byte === packet[31]) {
        first_receive = 1;
        last_receive = Date.now(); // Using milliseconds since UNIX epoch
        receive_start_detect = 1;

        // Extract telemetry data
        let error = packet[0];
        let flight_mode = packet[1];
        let flight_mode_str = "";
        if (flight_mode == 1) flight_mode_str = "1-Auto level";
        if (flight_mode == 2) flight_mode_str = "2-Altutude hold";
        if (flight_mode == 3) flight_mode_str = "3-GPS hold";
        if (flight_mode == 4) flight_mode_str = "4-RTH active";
        if (flight_mode == 5) flight_mode_str = "5-RTH Increase altitude";
        if (flight_mode == 6) flight_mode_str = "6-RTH Returning to home position";
        if (flight_mode == 7) flight_mode_str = "7-RTH Landing";
        if (flight_mode == 8) flight_mode_str = "8-RTH finished";
        if (flight_mode == 9) flight_mode_str = "9-Fly to waypoint";


        let battery_voltage = packet[2] / 10.0;
        let battery_bar_level = packet[2];

        // convert int16_t to num
        var temperature = (packet[4] << 8) | packet[3];
        if (temperature & 0x8000) {
          temperature = temperature - 0x10000;
        }
        // convert to Celsius, format to two decimal places

        temperature = (temperature / 340.0 + 36.53);
        temperature = temperature.toFixed(2);

        let roll_angle = packet[5] - 100;
        let pitch_angle = packet[6] - 100;
        let start = packet[7];
        let altitude_meters = (packet[8] | (packet[9] << 8)) - 1000;
        max_altitude_meters = Math.max(max_altitude_meters, altitude_meters);
        let takeoff_throttle = packet[10] | (packet[11] << 8);
        let actual_compass_heading = packet[12] | (packet[13] << 8);
        let heading_lock = packet[14];
        let number_used_sats = packet[15];
        let fix_type = packet[16];
        let lat = (packet[17] | (packet[18] << 8) | (packet[19] << 16) | (packet[20] << 24)) / 1000000.0;
        let lon = (packet[21] | (packet[22] << 8) | (packet[23] << 16) | (packet[24] << 24)) / 1000000.0;
        let adjustable_setting_1 = (packet[25] | (packet[26] << 8)) / 100.0;
        let adjustable_setting_2 = (packet[27] | (packet[28] << 8)) / 100.0;
        let adjustable_setting_3 = (packet[29] | (packet[30] << 8)) / 100.0;

        // Calculate ground distance
        let ground_distance_x = Math.pow(((lat - home_lat_gps) ** 2) * 0.111, 2);
        let ground_distance_y = Math.pow((lon - home_lon_gps) * Math.cos((lat / 1000000) * 0.017453) * 0.111, 2);
        let ground_distance = Math.sqrt(ground_distance_x + ground_distance_y);

        // Calculate line-of-sight (LOS) distance
        let los_distance = Math.sqrt(ground_distance ** 2 + altitude_meters ** 2);

        // Return the structured telemetry data
        return {
          error,
          flight_mode,
          battery_voltage,
          battery_bar_level,
          temperature,
          roll_angle,
          pitch_angle,
          start,
          altitude_meters,
          max_altitude_meters,
          takeoff_throttle,
          actual_compass_heading,
          heading_lock,
          number_used_sats,
          fix_type,
          lat,
          lon,
          adjustable_setting_1,
          adjustable_setting_2,
          adjustable_setting_3,
          ground_distance,
          los_distance,
        };
      }
      else
      {
        console.error('Check byte mismatch');
        return null;
      }
    }
}

let sendCounter = 0;
let sendTimer = 0;
function sendTelemetryData(lat, lon) {
  // clear interval if it exists
  sendCounter = 0;
  clearInterval(sendTimer);

  // generate packet
  var buff = generateWRPacket(lat, lon);
  
  // send data, we check controller recieves it
  console.log('Send data:', lat, lon, buff);
  function gotData(event) {
    if (event.detail && event.detail.flight_mode == 9) {
      console.log('Received data:', event.detail);
      clearInterval(sendTimer);
    }
  };

  // subscribe to telemetry
  document.addEventListener('telemetry', gotData);
  function trySend() {
    if (sendCounter < 1) {
      console.log('Try attempt:', sendCounter);
      ws.send(buff);
      sendCounter++;
      setTimeout(trySend, 1000);
    } else {
      // data sent, cleanup
      clearInterval(sendTimer);
      document.removeEventListener('telemetry', gotData);
    }
  }

  // try to send every second, until sent or 10 attempts
  sendTimer = setTimeout(trySend, 1000);
}

function generateWRPacket(lat, lon) {
  let send_buffer = new Uint8Array(13);

  send_buffer[0] = 'W'.charCodeAt(0);
  send_buffer[1] = 'P'.charCodeAt(0);

  let click_lat = Math.floor(lat * 1000000);
  let click_lon = Math.floor(lon * 1000000);

  send_buffer[5] = (click_lat >> 24) & 0xFF;
  send_buffer[4] = (click_lat >> 16) & 0xFF;
  send_buffer[3] = (click_lat >> 8) & 0xFF;
  send_buffer[2] = click_lat & 0xFF;

  send_buffer[9] = (click_lon >> 24) & 0xFF;
  send_buffer[8] = (click_lon >> 16) & 0xFF;
  send_buffer[7] = (click_lon >> 8) & 0xFF;
  send_buffer[6] = click_lon & 0xFF;

  send_buffer[10] = '-'.charCodeAt(0);

  let check_byte = 0;
  for (let temp_byte = 0; temp_byte <= 10; temp_byte++) {
    check_byte ^= send_buffer[temp_byte];
  }
  send_buffer[11] = check_byte;

  return send_buffer;
}