﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UnitsNet;
using Windows.Devices.Gpio;

namespace Glovebox.IoT.Devices.Sensors.Distance
{
    public class HCSR04
    {
        private GpioController gpio = GpioController.GetDefault();

        private GpioPin trig;
        private GpioPin echo;

        private readonly int trig_Pin;
        private readonly int echo_Pin;


        static Object deviceLock = new object();

        Stopwatch sw = new Stopwatch();

        public int TimeoutMilliseconds { get; set; } = 20;

        public HCSR04(byte trig_Pin, byte echo_Pin, int timeoutMilliseconds)
        {

            this.trig_Pin = trig_Pin;
            this.echo_Pin = echo_Pin;

            TimeoutMilliseconds = timeoutMilliseconds;

            trig = gpio.OpenPin(trig_Pin);
            echo = gpio.OpenPin(echo_Pin);

            trig.SetDriveMode(GpioPinDriveMode.Output);
            echo.SetDriveMode(GpioPinDriveMode.Input);

            trig.Write(GpioPinValue.Low);
        }

        public Length GetDistance()
        {
            return GetDistance(TimeoutMilliseconds);
        }

        /// <summary>
        /// Measures distance in centimeters
        /// </summary>
        /// <param name="timeoutMilliseconds">20 milliseconds is enough time to measure up to 3 meters</param>
        /// <returns></returns>
        public Length GetDistance(int timeoutMilliseconds=20)
        {
            lock (deviceLock)
            {

                //http://www.c-sharpcorner.com/UploadFile/167ad2/how-to-use-ultrasonic-sensor-hc-sr04-in-arduino/
                //http://www.modmypi.com/blog/hc-sr04-ultrasonic-range-sensor-on-the-raspberry-pi


                trig.Write(GpioPinValue.Low);                     // ensure the trigger is off
                Task.Delay(TimeSpan.FromMilliseconds(1)).Wait();  // wait for the sensor to settle

                trig.Write(GpioPinValue.High);                          // turn on the pulse
                Task.Delay(TimeSpan.FromMilliseconds(.01)).Wait();      // let the pulse run for 10 microseconds
                trig.Write(GpioPinValue.Low);                           // turn off the pulse

                var time = PulseIn(echo, GpioPinValue.High, timeoutMilliseconds);

                // speed of sound is 34300 cm per second or 34.3 cm per millisecond
                // since the sound waves traveled to the obstacle and back to the sensor
                // I am dividing by 2 to represent travel time to the obstacle

                

                return Length.FromCentimeters(time * 34.3 / 2.0); // at 20 degrees at sea level
            }
        }

        private double PulseIn(GpioPin pin, GpioPinValue value, int timeout)
        {
            sw.Restart();

            // Wait for pulse
            while (sw.ElapsedMilliseconds < timeout && pin.Read() != value) { }

            if (sw.ElapsedMilliseconds >= timeout)
            {
                sw.Stop();
                return 0;
            }
            sw.Restart();

            // Wait for pulse end
            while (sw.ElapsedMilliseconds < timeout && pin.Read() == value) { }

            sw.Stop();

            return sw.ElapsedMilliseconds < timeout ? sw.Elapsed.TotalMilliseconds : 0;
        }
    }
}
