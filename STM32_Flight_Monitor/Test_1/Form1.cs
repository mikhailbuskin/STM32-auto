using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Media;
using System.IO.Ports;
using System.Globalization;
using System.Diagnostics;

///////////////////////////////////////////////////////////////////////////////////////
//Terms of use
///////////////////////////////////////////////////////////////////////////////////////
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.
///////////////////////////////////////////////////////////////////////////////////////
//Safety note
///////////////////////////////////////////////////////////////////////////////////////
//Always remove the propellers and stay away from the motors unless you
//are 100% certain of what you are doing.
///////////////////////////////////////////////////////////////////////////////////////

namespace Test_1
{
    public partial class Scherm_1 : Form
    {
        static SerialPort port;
        public Scherm_1()
        {
            InitializeComponent();
            port = new SerialPort();
            ((Control)webBrowser1).Enabled = false;
            ((Control)webBrowser2).Enabled = false;
            webBrowser1.Navigate("https://www.openstreetmap.org/#map=2/50.8/5.6");
            webBrowser1.Visible = true;
            webBrowser2.Visible = false;
            received_data = 2;
            textBox10.Text = (zoom - 14).ToString();
        }

        private void ComboBox1_Click(object sender, EventArgs e)
        {
            string[] ports = SerialPort.GetPortNames();
            PortNumber.Items.Clear();
            PortNumber.Items.AddRange(ports);
        }

        private void DataReceivedHandler(object sender, EventArgs e)
        {
            if (received_data == 0) received_data = 1;
            int nextByte = port.ReadByte();
            if (nextByte >= 0) receive_buffer[receive_buffer_counter] = (byte)nextByte;                 //Load them in the received_buffer array.
                                                                                                        //Search for the start signature in the received data stream.
            if (receive_byte_previous == 'J' && receive_buffer[receive_buffer_counter] == 'B')
            {
                receive_buffer_counter = 0;                                           //Reset the receive_buffer_counter counter if the start signature if found.
                receive_start_detect++;                                              //Increment the receive_start_detect to check for a full data stream reception.
                if (receive_start_detect >= 2)
                {
                    get_data();
                }
            }
            else
            {                                                                   //If there is no start signature detected.
                receive_byte_previous = receive_buffer[receive_buffer_counter];       //Safe the current received byte for the next loop.
                receive_buffer_counter++;                                            //Increment the receive_buffer_counter variable.
                if (receive_buffer_counter > 48) receive_buffer_counter = 0;            //Reset the receive_buffer_counter variable when it becomes larger than 38.
            }
        }

        private void OpenClose_Click(object sender, EventArgs e)
        {
            if (OpenClose.Text == "Open")
            {
                if (PortNumber.Text.Length > 1)
                {
                    port = new SerialPort(PortNumber.Text, 9600, Parity.None, 8, StopBits.One);
                    port.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

                    port.Open();
                    OpenClose.Text = "Close";
                    Location_update_timer.Enabled = true;
                }
                else
                {
                    string message = "No port selected";
                    string title = "Error";
                    MessageBox.Show(message, title);
                    return;
                }
            }
            else
            {
                clear_waypoint_labels();
                port.Close();
                OpenClose.Text = "Open";
                Location_update_timer.Enabled = false;
                webBrowser1.Navigate("https://www.openstreetmap.org/#map=2/50.8/5.6");
                webBrowser1.Visible = true;
                webBrowser2.Visible = false;
                label12.Visible = false;
                label24.Visible = false;
                first_receive = 0;
                button6.Enabled = false;
                flight_timer.Enabled = false;
            }
        }

        private void get_data()
        {
            check_byte = 0;
            for (temp_byte = 0; temp_byte <= 30; temp_byte++) check_byte ^= receive_buffer[temp_byte];
            if (check_byte == receive_buffer[31])
            {
                first_receive = 1;
                last_receive = milliseconds;
                receive_start_detect = 1;

                error = receive_buffer[0];
                flight_mode = receive_buffer[1];
                battery_voltage = (float)receive_buffer[2] / 10.0f;
                battery_bar_level = receive_buffer[2];

                temperature = (short)(receive_buffer[3] | receive_buffer[4] << 8);
                roll_angle = receive_buffer[5] - 100;
                pitch_angle = receive_buffer[6] - 100;
                start = receive_buffer[7];
                altitude_meters = (receive_buffer[8] | receive_buffer[9] << 8) - 1000;
                if (altitude_meters > max_altitude_meters) max_altitude_meters = altitude_meters;
                takeoff_throttle = receive_buffer[10] | receive_buffer[11] << 8;
                actual_compass_heading = receive_buffer[12] | receive_buffer[13] << 8;
                heading_lock = receive_buffer[14];
                number_used_sats = receive_buffer[15];
                fix_type = receive_buffer[16];
                l_lat_gps = (int)receive_buffer[17] | (int)receive_buffer[18] << 8 | (int)receive_buffer[19] << 16 | (int)receive_buffer[20] << 24;
                l_lon_gps = (int)receive_buffer[21] | (int)receive_buffer[22] << 8 | (int)receive_buffer[23] << 16 | (int)receive_buffer[24] << 24;

                adjustable_setting_1 = (float)(receive_buffer[25] | receive_buffer[26] << 8) / 100.0f;
                adjustable_setting_2 = (float)(receive_buffer[27] | receive_buffer[28] << 8) / 100.0f;
                adjustable_setting_3 = (float)(receive_buffer[29] | receive_buffer[30] << 8) / 100.0f;
                ground_distance = Math.Pow((float)((l_lat_gps - home_lat_gps) ^ 2) * 0.111, 2);
                ground_distance += Math.Pow((float)(l_lon_gps - home_lon_gps) * (Math.Cos((l_lat_gps / 1000000) * 0.017453) * 0.111), 2);
                ground_distance = Math.Sqrt(ground_distance);
                los_distance = Math.Sqrt(Math.Pow(ground_distance, 2) + Math.Pow(altitude_meters, 2));

               
            }
        }

        private void Timer2_Tick(object sender, EventArgs e)
        {
            if (start == 2 && flight_mode == 3 && fly_waypoint_list.Enabled == false) button6.Enabled = true;
            else button6.Enabled = false;

            if (start != 2) clear_waypoint_labels();
            

            if(port.IsOpen && first_receive == 0)
            {
                label27.Visible = true;
            }
            else label27.Visible = false;
            
            milliseconds += 100;
            if (first_receive == 1)
            {
                if (milliseconds - last_receive > 2000 && label24.Visible == false) label24.Visible = true;
                if (milliseconds - last_receive < 1000 && label24.Visible == true) label24.Visible = false;

                if (flight_mode == 1) textBox2.Text = "1-Auto level";
                if (flight_mode == 2) textBox2.Text = "2-Altutude hold";
                if (flight_mode == 3) textBox2.Text = "3-GPS hold";
                if (flight_mode == 4) textBox2.Text = "4-RTH active";
                if (flight_mode == 5) textBox2.Text = "5-RTH Increase altitude";
                if (flight_mode == 6) textBox2.Text = "6-RTH Returning to home position";
                if (flight_mode == 7) textBox2.Text = "7-RTH Landing";
                if (flight_mode == 8) textBox2.Text = "8-RTH finished";
                if (flight_mode == 9) textBox2.Text = "9-Fly to waypoint";


                if (start == 0)
                {
                    pictureBox1.Visible = true;
                    pictureBox2.Visible = false;
                    pictureBox3.Visible = false;
                }
                if (start == 1)
                {
                    pictureBox1.Visible = false;
                    pictureBox2.Visible = false;
                    pictureBox3.Visible = true;
                }
                if (start == 2)
                {
                    pictureBox1.Visible = false;
                    pictureBox2.Visible = true;
                    pictureBox3.Visible = false;
                }

                    if (error == 0) textBox3.Text = "No error";
                if (error == 1) textBox3.Text = "Battery LOW";
                if (error == 2) textBox3.Text = "Program loop time";
                if (error == 3) textBox3.Text = "ACC cal error";
                if (error == 4) textBox3.Text = "GPS watchdog time";
                if (error == 5) textBox3.Text = "Manual take-off thr error";
                if (error == 6) textBox3.Text = "No take-off detected";
                if (error == 7) textBox3.Text = "Auto throttle error";

                label15.Text = number_used_sats.ToString();
                if (number_used_sats > 6)
                {
                    pictureBox4.Visible = false;
                    pictureBox5.Visible = false;
                    pictureBox6.Visible = true;

                }
                else if (number_used_sats > 3)
                {
                    pictureBox4.Visible = false;
                    pictureBox5.Visible = true;
                    pictureBox6.Visible = false;

                }
                else
                {
                    pictureBox4.Visible = true;
                    pictureBox5.Visible = false;
                    pictureBox6.Visible = false;

                }




                label16.Text = ((float)l_lat_gps / 1000000.0).ToString(new CultureInfo("en-US"));
                label17.Text = ((float)l_lon_gps / 1000000.0).ToString(new CultureInfo("en-US"));
                label18.Text = actual_compass_heading.ToString();
                label19.Text = altitude_meters.ToString() + "m";
                label20.Text = max_altitude_meters.ToString() + "m";
                label21.Text = battery_voltage.ToString("00.0") + "V";
                label4.Text = pitch_angle.ToString();
                label14.Text = roll_angle.ToString();
                label37.Text = ((float)temperature / 340.0 + 36.53).ToString("00.0") + "C";

                if (battery_bar_level > 124) battery_bar_level = 124;
                if (battery_bar_level < 85) battery_bar_level = 85;
                if (battery_bar_level > 108) panel5.BackColor = Color.Lime;
                else if (battery_bar_level > 100) panel5.BackColor = Color.Yellow;
                else panel5.BackColor = Color.Red;

                panel6.Size = new Size(34,134 - ((battery_bar_level - 80)*3));

                if (home_gps_set == 1) label22.Text = los_distance.ToString("0.") + "m";
                else label22.Text = "0m";

                if (home_gps_set == 0 && number_used_sats > 4 && start == 2)
                {
                    home_gps_set = 1;
                    home_lat_gps = l_lat_gps;
                    home_lon_gps = l_lon_gps;
                    label12.Visible = true;
                }
                if (home_gps_set == 1 && start == 0)
                {
                    home_gps_set = 0;
                    label12.Visible = false;
                }
                if(start == 2)
                {
                    if (flight_timer.Enabled == false) flight_timer.Enabled = true;
                }
                if(start == 0)
                {
                    if (flight_timer.Enabled == true) flight_timer.Enabled = false;
                }
            }
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            webbrouwser_1_ready = 1;
        }

        private void webBrowser2_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            webbrouwser_2_ready = 1;
        }

        private void Location_update_timer_Tick(object sender, EventArgs e)
        {
            if (number_used_sats > 4 && l_lat_gps != 0)
            {
                if (home_gps_set == 0)
                {
                    location_address = "https://www.openstreetmap.org/?mlat="
                    + ((float)(l_lat_gps) / 1000000.0).ToString(new CultureInfo("en-US"))
                    + "&mlon="
                    + ((float)l_lon_gps / 1000000.0).ToString(new CultureInfo("en-US"))
                    + "&zoom="
                    + zoom.ToString();

                }
                else
                {

                    location_address = "https://www.openstreetmap.org/?mlat="
                    + ((float)(l_lat_gps) / 1000000.0).ToString(new CultureInfo("en-US"))
                    + "&mlon="
                    + ((float)l_lon_gps / 1000000.0).ToString(new CultureInfo("en-US"))
                    + "#map="
                    + zoom.ToString()
                    + "/"
                    + ((float)(home_lat_gps) / 1000000.0).ToString(new CultureInfo("en-US"))
                    + "/"
                    + ((float)(home_lon_gps) / 1000000.0).ToString(new CultureInfo("en-US"));


                }
                if (webbrouwser_active == 1 && webbrouwser_2_ready == 1)
                {
                    webBrowser2.Visible = true;
                    webBrowser1.Visible = false;
                    webbrouwser_1_ready = 0;
                    webBrowser1.Navigate(location_address);
                    webbrouwser_active = 0;
                }
                else if (webbrouwser_active == 0 && webbrouwser_1_ready == 1)
                {
                    webBrowser1.Visible = true;
                    webBrowser2.Visible = false;
                    webbrouwser_2_ready = 0;
                    webBrowser2.Navigate(location_address);
                    webbrouwser_active = 1;
                }
            }
        }

        private void Rx_timer_blink_Tick(object sender, EventArgs e)
        {
            if (received_data > 0) received_data++;

            if (received_data == 2)
            {
                indicator_on();
            }
            if (received_data == 3)
            {
                indicator_off();
            }
            if (received_data == 10)
            {
                received_data = 0;
            }
        }

        private void clear_waypoint_labels()
        {
            label23.Visible = false;
            label28.Visible = false;
            label29.Visible = false;
            label30.Visible = false;
            label31.Visible = false;
            label32.Visible = false;
            label33.Visible = false;
            label34.Visible = false;
            label35.Visible = false;
            button1.Enabled = true;
            button4.Enabled = true;
        }

        private void indicator_on()
        {
            Graphics g = panel1.CreateGraphics();
            Pen p = new Pen(Color.Blue);
            SolidBrush sb = new SolidBrush(Color.LightBlue);
            g.DrawEllipse(p, 1, 1, 10, 10);
            g.FillEllipse(sb, 1, 1, 10, 10);
        }

        private void indicator_off()
        {
            Graphics g = panel1.CreateGraphics();
            Pen p = new Pen(Color.DarkBlue);
            SolidBrush sb = new SolidBrush(Color.DarkBlue);
            g.DrawEllipse(p, 1, 1, 10, 10);
            g.FillEllipse(sb, 1, 1, 10, 10);
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            if (zoom > 14) zoom--;
            textBox10.Text = (zoom - 14).ToString();
        }

        private void Button4_Click(object sender, EventArgs e)
        {
            if (zoom < 19) zoom++;
            textBox10.Text = (zoom - 14).ToString();
        }

        private void Scherm_1_MouseDown(object sender, MouseEventArgs e)
        {
            click_lat = e.Y;
            click_lon = e.X;
            if (port.IsOpen && first_receive == 1 && start == 2 && flight_mode == 3 && fly_waypoint_list.Enabled == false)
            {
                if (click_lat > webBrowser1.Location.Y && click_lat < webBrowser1.Location.Y + webBrowser1.Size.Height
                && click_lon > webBrowser1.Location.X && click_lon < webBrowser1.Location.X + webBrowser1.Size.Width)
                {
                    if (create_waypoint_list && waypoint_list_counter < 8)
                    {
                        if (waypoint_list_counter == 0)
                        {
                            label28.Location = new Point(click_lon - 8, click_lat - 8);
                            label28.Visible = true;
                        }
                        if (waypoint_list_counter == 1)
                        {
                            label29.Location = new Point(click_lon - 8, click_lat - 8);
                            label29.Visible = true;
                        }
                        if (waypoint_list_counter == 2)
                        {
                            label30.Location = new Point(click_lon - 8, click_lat - 8);
                            label30.Visible = true;
                        }
                        if (waypoint_list_counter == 3)
                        {
                            label31.Location = new Point(click_lon - 8, click_lat - 8);
                            label31.Visible = true;
                        }
                        if (waypoint_list_counter == 4)
                        {
                            label32.Location = new Point(click_lon - 8, click_lat - 8);
                            label32.Visible = true;
                        }
                        if (waypoint_list_counter == 5)
                        {
                            label33.Location = new Point(click_lon - 8, click_lat - 8);
                            label33.Visible = true;
                        }
                        if (waypoint_list_counter == 6)
                        {
                            label34.Location = new Point(click_lon - 8, click_lat - 8);
                            label34.Visible = true;
                        }
                        if (waypoint_list_counter == 7)
                        {
                            label35.Location = new Point(click_lon - 8, click_lat - 8);
                            label35.Visible = true;
                        }
                    }
                    else
                    {
                        label23.Location = new Point(click_lon - 8, click_lat - 8);
                        label23.Visible = true;
                    }
                    button1.Enabled = false;
                    button4.Enabled = false;


                    click_lat -= 35;
                    click_lat -= (webBrowser1.Size.Height / 2) + webBrowser1.Location.Y;
                    click_lon -= (webBrowser1.Size.Width / 2) + webBrowser1.Location.X;
                    if (zoom - 14 == 5)
                    {
                        click_lat = (int)((float)click_lat * 1.664);
                        click_lon = (int)((float)click_lon * (1.664 / Math.Cos((l_lat_gps / 1000000) * 0.017453)));
                    }
                    if (zoom - 14 == 4)
                    {
                        click_lat = (int)((float)click_lat * 3.328);
                        click_lon = (int)((float)click_lon * (3.328 / Math.Cos((l_lat_gps / 1000000) * 0.017453)));
                    }
                    if (zoom - 14 == 3)
                    {
                        click_lat = (int)((float)click_lat * 6.656);
                        click_lon = (int)((float)click_lon * (6.656 / Math.Cos((l_lat_gps / 1000000) * 0.017453)));
                    }
                    if (zoom - 14 == 2)
                    {
                        click_lat = (int)((float)click_lat * 13.312);
                        click_lon = (int)((float)click_lon * (13.312 / Math.Cos((l_lat_gps / 1000000) * 0.017453)));
                    }
                    if (zoom - 14 == 1)
                    {
                        click_lat = (int)((float)click_lat * 26.624);
                        click_lon = (int)((float)click_lon * (26.624 / Math.Cos((l_lat_gps / 1000000) * 0.017453)));
                    }
                    if (zoom - 14 == 0)
                    {
                        click_lat = (int)((float)click_lat * 53.248);
                        click_lon = (int)((float)click_lon * (53.248 / Math.Cos((l_lat_gps / 1000000) * 0.017453)));
                    }

                    click_lat *= -1;
                    click_lat = home_lat_gps + click_lat;
                    click_lon = home_lon_gps + click_lon;

                    send_buffer[0] = (byte)'W';
                    send_buffer[1] = (byte)'P';

                    send_buffer[5] = (byte)(click_lat >> 24);
                    send_buffer[4] = (byte)(click_lat >> 16);
                    send_buffer[3] = (byte)(click_lat >> 8);
                    send_buffer[2] = (byte)(click_lat);

                    send_buffer[9] = (byte)(click_lon >> 24);
                    send_buffer[8] = (byte)(click_lon >> 16);
                    send_buffer[7] = (byte)(click_lon >> 8);
                    send_buffer[6] = (byte)(click_lon);

                    send_buffer[10] = (byte)'-';
                    check_byte = 0;
                    for (temp_byte = 0; temp_byte <= 10; temp_byte++)
                    {
                        check_byte ^= send_buffer[temp_byte];
                    }
                    send_buffer[11] = check_byte;

                    if (create_waypoint_list)
                    {
                        if (waypoint_list_counter < 8) waypoint_list_counter++;
                        waypoint_click_lat[waypoint_list_counter] = click_lat;
                        waypoint_click_lon[waypoint_list_counter] = click_lon;
                    }
                    else
                    {
                        if (port.IsOpen)
                        {
                            port.Write(send_buffer, 0, 13);
                        }
                        label26.Text = "Try 1";
                        new_telemetry_data_to_send = 1;
                        Send_telemetry_data.Enabled = true;
                    }


                }
            }
        }

        private void Send_telemetry_data_Tick(object sender, EventArgs e)
        {

            if (flight_mode == 3 && new_telemetry_data_to_send > 0 && new_telemetry_data_to_send <= 10)
            {
                if (port.IsOpen)
                {
                    port.Write(send_buffer, 0, 13);
                    new_telemetry_data_to_send++;
                    label26.Text = "Try " + new_telemetry_data_to_send.ToString();

                }
            }
            else
            {
                new_telemetry_data_to_send = 0;
                Send_telemetry_data.Enabled = false;
                if (flight_mode == 3) label26.Text = "Fail";
                if (flight_mode == 9)
                {
                    label26.Text = "Received";
                    if (waypoint_send_step == 2) waypoint_send_step = 3;
                }
            }
        }

        private void BT_reset_waypoints(object sender, EventArgs e)
        {
            clear_waypoint_labels();
            create_waypoint_list = false;
            fly_waypoint_list.Enabled = false;
            Send_telemetry_data.Enabled = false;
            label26.Text = "-";
        }

        private void Button6_Click(object sender, EventArgs e)
        {
            clear_waypoint_labels();
            create_waypoint_list = true;
            waypoint_list_counter = 0;
        }

        private void Fly_waypoint_list_Tick(object sender, EventArgs e)
        {
            if(flight_mode != 3 && flight_mode != 9)
            {
                fly_waypoint_list.Enabled = false;
                label26.Text = "Aborted";
            }
            
            if (waypoint_send_step == 1) {
                click_lat = waypoint_click_lat[send_telemetry_data_counter];
                click_lon = waypoint_click_lon[send_telemetry_data_counter];

                new_telemetry_data_to_send = 1;

                send_buffer[0] = (byte)'W';
                send_buffer[1] = (byte)'P';

                send_buffer[5] = (byte)(click_lat >> 24);
                send_buffer[4] = (byte)(click_lat >> 16);
                send_buffer[3] = (byte)(click_lat >> 8);
                send_buffer[2] = (byte)(click_lat);

                send_buffer[9] = (byte)(click_lon >> 24);
                send_buffer[8] = (byte)(click_lon >> 16);
                send_buffer[7] = (byte)(click_lon >> 8);
                send_buffer[6] = (byte)(click_lon);

                send_buffer[10] = (byte)'-';
                check_byte = 0;
                for (temp_byte = 0; temp_byte <= 10; temp_byte++)
                {
                    check_byte ^= send_buffer[temp_byte];
                }
                send_buffer[11] = check_byte;

                Send_telemetry_data.Enabled = true;
                waypoint_send_step = 2;
            }
            if(waypoint_send_step == 3)
            {
                if (flight_mode == 3) waypoint_send_step = 4;
            }
            if (waypoint_send_step == 4)
            {
                if (waypoint_list_counter == send_telemetry_data_counter)
                {
                    label26.Text = "Waypoints ready";
                    fly_waypoint_list.Enabled = false;
                }
                else
                {
                    waypoint_send_step = 1;
                    send_telemetry_data_counter++;
                }
            }


        }

        private void Button7_Click(object sender, EventArgs e)
        {
            send_telemetry_data_counter = 1;
            waypoint_send_step = 1;
            fly_waypoint_list.Enabled = true;
        }

        private void Flight_timer_Tick(object sender, EventArgs e)
        {
            flight_timer_seconds++;
            label36.Text = ("00:" + (flight_timer_seconds / 60).ToString("00.") + ":" + (flight_timer_seconds % 60).ToString("00."));
        }
    }
}
