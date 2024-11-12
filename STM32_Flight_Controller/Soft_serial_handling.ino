
void si_translate_bytes(void) {
  if (  millis() - si_last_input_change > 50 && si_print_flag == 0) {
    si_time_array[si_time_array_counter] = si_time_array[si_time_array_counter - 1] + 104;

    si_level = 0;
    si_received_bytes_counter = 0;
    si_time_array_counter_2 = 1;
    si_byte_counter = 0;

    while (si_received_bytes_counter <= 130) {

      if (5.0 + 104.1666667 * (float)si_received_bytes_counter > si_time_array[si_time_array_counter_2]) { //If bit time is lower than the level change time
        if (si_level)si_level = 0;
        else si_level = 1;
        si_time_array_counter_2 ++;
      }

      if (si_received_bytes_counter == 1 + si_byte_counter * 10)si_received_bytes[si_byte_counter] = si_level << 0;
      if (si_received_bytes_counter == 2 + si_byte_counter * 10)si_received_bytes[si_byte_counter] += si_level << 1;
      if (si_received_bytes_counter == 3 + si_byte_counter * 10)si_received_bytes[si_byte_counter] += si_level << 2;
      if (si_received_bytes_counter == 4 + si_byte_counter * 10)si_received_bytes[si_byte_counter] += si_level << 3;
      if (si_received_bytes_counter == 5 + si_byte_counter * 10)si_received_bytes[si_byte_counter] += si_level << 4;
      if (si_received_bytes_counter == 6 + si_byte_counter * 10)si_received_bytes[si_byte_counter] += si_level << 5;
      if (si_received_bytes_counter == 7 + si_byte_counter * 10)si_received_bytes[si_byte_counter] += si_level << 6;
      if (si_received_bytes_counter == 8 + si_byte_counter * 10)si_received_bytes[si_byte_counter] += si_level << 7;
      if (si_received_bytes_counter == 9 + si_byte_counter * 10)si_byte_counter ++;
      si_received_bytes_counter ++;
    }

    si_check_byte = 0x00;
    for (count_var = 0; count_var <= 10; count_var++)si_check_byte ^= si_received_bytes[count_var];

    if (si_check_byte == si_received_bytes[11]) {
      if (si_received_bytes[0] == 'W' && si_received_bytes[1] == 'P') {
        new_waypoint_available = 1;
        si_received_bytes[0] = 0x00;
        wp_lat_gps = (int32_t)si_received_bytes[2] | (int32_t)si_received_bytes[3] << 8 | (int32_t)si_received_bytes[4] << 16 | (int32_t)si_received_bytes[5] << 24;
        wp_lon_gps = (int32_t)si_received_bytes[6] | (int32_t)si_received_bytes[7] << 8 | (int32_t)si_received_bytes[8] << 16 | (int32_t)si_received_bytes[9] << 24;
        if (waypoint_set == 1 && home_point_recorded == 1 && flight_mode == 3) {
          fly_to_new_waypoint = 1;
          fly_to_new_waypoint_step = 0;
          fly_to_waypoint_lat_factor = 0;
          fly_to_waypoint_lon_factor = 0;
        }
      }
    }
    si_print_flag = 1;
  }

  if (millis() - si_last_input_change < 4 && si_print_flag == 1) {
    si_print_flag = 0;
  }
}

void Serial_input_handler(void) {
  if (si_rising_edge_set) {                                    //If the receiver channel 1 input pulse on A0 is high.
    TIMER3_BASE->CCER |= TIMER_CCER_CC1P;                   //Change the input capture mode to the falling edge of the pulse.
    si_rising_edge_set = 0;
  }
  else {
    TIMER3_BASE->CCER &= ~TIMER_CCER_CC1P;                  //Change the input capture mode to the rising edge of the pulse.
    si_rising_edge_set = 1;
  }

  si_measured_time = TIMER3_BASE->CCR1 - si_measured_time_start;
  if (si_measured_time < 0)si_measured_time += 0xFFFF;

  si_last_input_change = millis();
  if (si_last_input_change - si_last_input_change_previous > 50) {
    si_time_array_counter = 1;
    si_measured_time_start = TIMER3_BASE->CCR1;
  }
  else {
    si_time_array[si_time_array_counter] = si_measured_time;
    if (si_time_array_counter < 180)si_time_array_counter++;
  }
  si_last_input_change_previous = si_last_input_change;
}

