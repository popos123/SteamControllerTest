﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamControllerTest.MapperUtil;

namespace SteamControllerTest.TouchpadActions
{
    public class TouchpadMouseJoystick : TouchpadMapAction
    {
        public class PropertyKeyStrings
        {
            public const string NAME = "Name";
            public const string DEAD_ZONE = "DeadZone";
            public const string MAX_ZONE = "MaxZone";
            public const string ANTIDEAD_ZONE = "AntiDeadZone";
            //public const string OUTPUT_CURVE = "OutputCurve";
            public const string OUTPUT_STICK = "OutputStick";
            //public const string ROTATION = "Rotation";
            //public const string INVERT_X = "InvertX";
            //public const string INVERT_Y = "InvertY";
            //public const string VERTICAL_SCALE = "VerticalScale";
            //public const string MAX_OUTPUT = "MaxOutput";
            //public const string MAX_OUTPUT_ENABLED = "MaxOutputEnabled";
            //public const string SQUARE_STICK_ENABLED = "SquareStickEnabled";
            //public const string SQUARE_STICK_ROUNDNESS = "SquareStickRoundness";
        }

        private HashSet<string> fullPropertySet = new HashSet<string>()
        {
            PropertyKeyStrings.NAME,
            PropertyKeyStrings.DEAD_ZONE,
            PropertyKeyStrings.MAX_ZONE,
            PropertyKeyStrings.ANTIDEAD_ZONE,
            //PropertyKeyStrings.OUTPUT_CURVE,
            PropertyKeyStrings.OUTPUT_STICK,
            //PropertyKeyStrings.INVERT_X,
            //PropertyKeyStrings.INVERT_Y,
            //PropertyKeyStrings.ROTATION,
            //PropertyKeyStrings.VERTICAL_SCALE,
            //PropertyKeyStrings.MAX_OUTPUT_ENABLED,
            //PropertyKeyStrings.MAX_OUTPUT,
            //PropertyKeyStrings.SQUARE_STICK_ENABLED,
            //PropertyKeyStrings.SQUARE_STICK_ROUNDNESS,
        };

        private OutputActionData outputAction;
        public OutputActionData OutputAction
        {
            get => outputAction;
        }

        private double xNorm = 0.0, yNorm = 0.0;
        private double xMotion;
        private double yMotion;

        private const int TRACKBALL_INIT_FRICTION = 10;
        private const int TRACKBALL_JOY_FRICTION = 7;
        private const int TRACKBALL_MASS = 45;
        private const double TRACKBALL_RADIUS = 0.0245;
        private const double TOUCHPAD_MOUSE_OFFSET = 0.375;
        //private const double TOUCHPAD_COEFFICIENT = 0.012;
        private const double TOUCHPAD_COEFFICIENT = 0.012 * 1.1;

        private double TRACKBALL_INERTIA = 2.0 * (TRACKBALL_MASS * TRACKBALL_RADIUS * TRACKBALL_RADIUS) / 5.0;
        //private double TRACKBALL_SCALE = 0.000023;
        private double TRACKBALL_SCALE = 0.000023;
        private const int TRACKBALL_BUFFER_LEN = 8;
        private double[] trackballXBuffer = new double[TRACKBALL_BUFFER_LEN];
        private double[] trackballYBuffer = new double[TRACKBALL_BUFFER_LEN];
        private int trackballBufferTail = 0;
        private int trackballBufferHead = 0;
        private double trackballAccel = 0.0;
        private double trackballXVel = 0.0;
        private double trackballYVel = 0.0;
        private bool trackballActive = false;
        private double trackballDXRemain = 0.0;
        private double trackballDYRemain = 0.0;

        public TouchpadMouseJoystick()
        {
            this.outputAction = new OutputActionData(OutputActionData.ActionType.GamepadControl, StickActionCodes.RS);

            trackballAccel = TRACKBALL_RADIUS * TRACKBALL_JOY_FRICTION / TRACKBALL_INERTIA;
        }

        public override void Prepare(Mapper mapper, ref TouchEventFrame touchFrame, bool alterState = true)
        {
            //Trace.WriteLine($"IN PREPARE {touchFrame.X} {touchFrame.Y}");
            //Trace.WriteLine($"IN PREPARE {touchFrame.X} {touchFrame.Y}");
            TrackballMouseJoystickProcess(mapper, ref touchFrame);

            active = activeEvent = true;
        }

        public override void Event(Mapper mapper)
        {
            double outXNorm = xNorm;
            double outYNorm = yNorm;

            //Trace.WriteLine($"OUTPUT: {outXNorm} {outYNorm}");

            outXNorm = mapper.FilterX.Filter(outXNorm, mapper.CurrentRate);
            outYNorm = mapper.FilterY.Filter(outYNorm, mapper.CurrentRate);

            mapper.GamepadFromStickInput(outputAction, outXNorm, outYNorm);

            if (outXNorm != 0.0 || outYNorm != 0.0)
            {
                active = true;
            }
            else
            {
                active = false;
            }

            activeEvent = false;
        }

        public override void Release(Mapper mapper, bool resetState = true)
        {
            xNorm = 0.0;
            yNorm = 0.0;

            mapper.GamepadFromStickInput(outputAction, 0.0, 0.0);

            PurgeTrackballData();

            active = activeEvent = false;
        }

        public override void SoftRelease(Mapper mapper, MapAction checkAction, bool resetState = true)
        {
            xNorm = 0.0;
            yNorm = 0.0;

            mapper.GamepadFromStickInput(outputAction, 0.0, 0.0);

            PurgeTrackballData();

            active = activeEvent = false;
        }

        private void PurgeTrackballData()
        {
            Array.Clear(trackballXBuffer, 0, TRACKBALL_BUFFER_LEN);
            Array.Clear(trackballYBuffer, 0, TRACKBALL_BUFFER_LEN);
            trackballXVel = 0.0;
            trackballYVel = 0.0;
            trackballActive = false;
            trackballBufferTail = 0;
            trackballBufferHead = 0;
            trackballDXRemain = 0.0;
            trackballDYRemain = 0.0;
        }

        private void TrackballMouseJoystickProcess(Mapper mapper, ref TouchEventFrame touchFrame)
        {
            ref TouchEventFrame previousTouchFrame =
                ref mapper.GetPreviousTouchEventFrame(touchpadDefinition.touchCode);

            if (touchFrame.Touch && !previousTouchFrame.Touch)
            {
                if (trackballActive)
                {
                    //Trace.WriteLine("CHECKING HERE");
                }

                // Initial touch
                Array.Clear(trackballXBuffer, 0, TRACKBALL_BUFFER_LEN);
                Array.Clear(trackballYBuffer, 0, TRACKBALL_BUFFER_LEN);
                trackballXVel = 0.0;
                trackballYVel = 0.0;
                trackballActive = false;
                trackballBufferTail = 0;
                trackballBufferHead = 0;
                trackballDXRemain = 0.0;
                trackballDYRemain = 0.0;

                //Trace.WriteLine("INITIAL");
            }
            else if (touchFrame.Touch && previousTouchFrame.Touch)
            {
                // Process normal mouse
                //RightTouchMouse(ref current, ref previous);
                //Trace.WriteLine("NORMAL");
                ProcessTouchMouseJoystick(ref touchFrame, ref previousTouchFrame);

            }
            else if (!touchFrame.Touch && previousTouchFrame.Touch)
            {
                // Initially released. Calculate velocity and start Trackball
                double currentWeight = 1.0;
                double finalWeight = 0.0;
                double x_out = 0.0, y_out = 0.0;
                int idx = -1;
                for (int i = 0; i < TRACKBALL_BUFFER_LEN && idx != trackballBufferHead; i++)
                {
                    idx = (trackballBufferTail - i - 1 + TRACKBALL_BUFFER_LEN) % TRACKBALL_BUFFER_LEN;
                    x_out += trackballXBuffer[idx] * currentWeight;
                    y_out += trackballYBuffer[idx] * currentWeight;
                    finalWeight += currentWeight;
                    currentWeight *= 1.0;
                }

                x_out /= finalWeight;
                trackballXVel = x_out;
                y_out /= finalWeight;
                trackballYVel = y_out;

                double dist = Math.Sqrt(trackballXVel * trackballXVel + trackballYVel * trackballYVel);
                if (dist >= 1.0)
                {
                    trackballActive = true;

                    //Trace.WriteLine($"START TRACK {dist}");
                    ProcessTrackballJoystickFrame(ref touchFrame);
                }
            }
            else if (!touchFrame.Touch && trackballActive)
            {
                //Trace.WriteLine("CONTINUE TRACK");
                // Trackball Running
                ProcessTrackballJoystickFrame(ref touchFrame);
            }
            else if (!touchFrame.Touch)
            {
                xNorm = yNorm = 0.0;
            }
        }

        private void ProcessTouchMouseJoystick(ref TouchEventFrame touchFrame, ref TouchEventFrame previousFrame)
        {
            int dx = touchFrame.X - previousFrame.X;
            int dy = -(touchFrame.Y - previousFrame.Y);
            //int rawDeltaX = dx, rawDeltaY = dy;

            //Trace.WriteLine(String.Format("DELTA X: {0} Y: {1}", dx, dy));

            // Fill trackball entry
            int iIndex = trackballBufferTail;
            trackballXBuffer[iIndex] = (dx * TRACKBALL_SCALE) / touchFrame.timeElapsed;
            trackballYBuffer[iIndex] = (dy * TRACKBALL_SCALE) / touchFrame.timeElapsed;
            trackballBufferTail = (iIndex + 1) % TRACKBALL_BUFFER_LEN;
            if (trackballBufferHead == trackballBufferTail)
                trackballBufferHead = (trackballBufferHead + 1) % TRACKBALL_BUFFER_LEN;

            TouchMouseJoystickPad(dx, -dy, ref touchFrame);
        }

        private void ProcessTrackballJoystickFrame(ref TouchEventFrame touchFrame)
        {
            double tempAngle = Math.Atan2(-trackballYVel, trackballXVel);
            double normX = Math.Abs(Math.Cos(tempAngle));
            double normY = Math.Abs(Math.Sin(tempAngle));
            int signX = Math.Sign(trackballXVel);
            int signY = Math.Sign(trackballYVel);

            double trackXvDecay = Math.Min(Math.Abs(trackballXVel), trackballAccel * touchFrame.timeElapsed * normX);
            double trackYvDecay = Math.Min(Math.Abs(trackballYVel), trackballAccel * touchFrame.timeElapsed * normY);
            double xVNew = trackballXVel - (trackXvDecay * signX);
            double yVNew = trackballYVel - (trackYvDecay * signY);
            double xMotion = (xVNew * touchFrame.timeElapsed) / TRACKBALL_SCALE;
            double yMotion = (yVNew * touchFrame.timeElapsed) / TRACKBALL_SCALE;
            if (xMotion != 0.0)
            {
                xMotion += trackballDXRemain;
            }
            else
            {
                trackballDXRemain = 0.0;
            }

            int dx = (int)xMotion;
            trackballDXRemain = xMotion - dx;

            if (yMotion != 0.0)
            {
                yMotion += trackballDYRemain;
            }
            else
            {
                trackballDYRemain = 0.0;
            }

            int dy = (int)yMotion;
            trackballDYRemain = yMotion - dy;

            trackballXVel = xVNew;
            trackballYVel = yVNew;

            //Trace.WriteLine(string.Format("DX: {0} DY: {1}", dx, dy));

            if (dx == 0 && dy == 0)
            {
                trackballActive = false;
                //Trace.WriteLine("ENDING TRACK");
                xNorm = yNorm = 0.0;
            }
            else
            {
                TouchMouseJoystickPad(dx, -dy, ref touchFrame);
            }
        }


        private void TouchMouseJoystickPad(int dx, int dy,
            ref TouchEventFrame touchFrame)
        {
            const int deadZone = 70;
            const int maxDeadZoneAxial = 100;
            const int minDeadZoneAxial = 20;

            //Trace.WriteLine(touchFrame.Y);

            int maxDirX = dx >= 0 ? 32767 : -32768;
            int maxDirY = dy >= 0 ? 32767 : -32768;

            double tempAngle = Math.Atan2(dy, dx);
            double normX = Math.Abs(Math.Cos(tempAngle));
            double normY = Math.Abs(Math.Sin(tempAngle));
            int signX = Math.Sign(dx);
            int signY = Math.Sign(dy);

            //double timeElapsed = touchFrame.timeElapsed;
            // Base speed 8 ms
            //double tempDouble = timeElapsed * 125.0;

            int maxValX = signX * 430;
            int maxValY = signY * 430;

            double xratio = 0.0, yratio = 0.0;
            double antiX = 0.30 * normX;
            double antiY = 0.30 * normY;

            int deadzoneX = (int)Math.Abs(normX * deadZone);
            int radialDeadZoneY = (int)(Math.Abs(normY * deadZone));
            //int deadzoneY = (int)(Math.Abs(normY * deadZone));

            int absDX = Math.Abs(dx);
            int absDY = Math.Abs(dy);

            // Check for radial dead zone first
            double mag = (dx * dx) + (dy * dy);
            if (mag <= (deadZone * deadZone))
            {
                dx = 0;
                dy = 0;
            }
            // Past radial. Check for bowtie
            else
            {
                //Trace.WriteLine($"X ({dx}) | Y ({dy})");

                // X axis calculated with scaled radial
                /*if (absDX > deadzoneX)
                {
                    dx -= signX * deadzoneX;
                    //dx = (dx < 0 && dx < maxValX) ? maxValX :
                    //    (dx > 0 && dx > maxValX) ? maxValX : dx;
                }
                else
                {
                    dx = 0;
                }

                if (absDY > radialDeadZoneY)
                {
                    dy -= signY * radialDeadZoneY;
                    //dy = (dy < 0 && dy < maxValY) ? maxValY :
                    //    (dy > 0 && dy > maxValY) ? maxValY : dy;
                }
                else
                {
                    dy = 0;
                }
                */

                // Need to adjust Y axis dead zone based on X axis input. Bowtie
                //int deadzoneY = Math.Max(radialDeadZoneY,
                //    (int)(Math.Min(1.0, absDX / (double)maxValX) * maxDeadZoneAxialY));
                double tempRangeRatioX = absDX / Math.Abs((double)maxValX);
                double tempRangeRatioY = absDY / Math.Abs((double)maxValY);

                int axialDeadX = (int)((maxDeadZoneAxial - minDeadZoneAxial) *
                    Math.Min(1.0, tempRangeRatioY) + minDeadZoneAxial);
                int deadzoneY = (int)((maxDeadZoneAxial - minDeadZoneAxial) *
                    Math.Min(1.0, tempRangeRatioX) + minDeadZoneAxial);

                //Trace.WriteLine($"Axial {axialDeadX} DX: {dx}");
                //Trace.WriteLine(deadzoneY);
                //int deadzoneY = Math.Min(maxValX, Math.Abs(dx)) * maxDeadZoneAxialY;
                //if (absDY > radialDeadZoneY)
                //{
                //    dy -= signY * radialDeadZoneY;
                //    dy = (dy < 0 && dy < maxValY) ? maxValY :
                //        (dy > 0 && dy > maxValY) ? maxValY : dy;
                //}
                //else if (absDY > deadzoneY)

                if (Math.Abs(dx) > axialDeadX)
                {
                    int tempUseDeadX = deadzoneX > axialDeadX ? deadzoneX : axialDeadX;
                    dx -= signX * tempUseDeadX;
                    double newMaxValX = Math.Abs(maxValX) - tempUseDeadX;
                    double scaleX = Math.Abs(dx) / (double)(newMaxValX);
                    dx = (int)(maxValX * scaleX);

                    dx = (dx < 0 && dx < maxValX) ? maxValX :
                        (dx > 0 && dx > maxValX) ? maxValX : dx;
                    //Trace.WriteLine($"{scaleX} {dx}");
                }
                else
                {
                    dx = 0;
                    //Trace.WriteLine("dx zero");
                }

                if (Math.Abs(dy) > deadzoneY)
                {
                    int tempUseDeadY = radialDeadZoneY > deadzoneY ? radialDeadZoneY : deadzoneY;
                    dy -= signY * tempUseDeadY;
                    double newMaxValY = Math.Abs(maxValY) - tempUseDeadY;
                    double scaleY = Math.Abs(dy) / (double)(newMaxValY);
                    dy = (int)(maxValY * scaleY);

                    dy = (dy < 0 && dy < maxValY) ? maxValY :
                        (dy > 0 && dy > maxValY) ? maxValY : dy;
                }
                else
                {
                    dy = 0;
                }

                /*
                if (absDY > deadzoneY)
                {
                    int newMaxValY = signY * (Math.Abs(maxValY) - deadzoneY);
                    dy -= signY * deadzoneY;
                    //dy = (dy < 0 && dy < maxValY) ? maxValY :
                    //    (dy > 0 && dy > maxValY) ? maxValY : dy;
                    dy = (dy < 0 && dy < newMaxValY) ? newMaxValY :
                          (dy > 0 && dy > newMaxValY) ? newMaxValY : dy;

                    double scaleY;
                    if (dy == newMaxValY)
                    {
                        scaleY = 1.0;
                    }
                    else
                    {
                        scaleY = (double)(dy - 0.0) / (double)(newMaxValY - 0.0);
                    }
                    //scaleY = (Math.Abs(newMaxValY) - Math.Abs(dy)) /
                    //    (double)(Math.Abs(maxValY) - 0);
                    dy = (int)(scaleY * maxValY);

                    //Trace.WriteLine($"SCALE: ({scaleY}) | NEW: ({newMaxValY}) | DY({dy})");
                }
                else
                {
                    dy = 0;
                }
                //*/
            }

            if (dx != 0) xratio = dx / (double)maxValX;
            if (dy != 0) yratio = dy / (double)maxValY;

            double maxOutRatio = 1.0;
            double maxOutXRatio = Math.Abs(Math.Cos(tempAngle)) * maxOutRatio;
            double maxOutYRatio = Math.Abs(Math.Sin(tempAngle)) * maxOutRatio;

            //Trace.WriteLine($"{maxOutXRatio} {maxOutYRatio}");
            // Expand output a bit
            maxOutXRatio = Math.Min(maxOutXRatio / 0.96, 1.0);
            // Expand output a bit
            maxOutYRatio = Math.Min(maxOutYRatio / 0.96, 1.0);

            xratio = Math.Min(Math.Max(xratio, 0.0), maxOutXRatio);
            yratio = Math.Min(Math.Max(yratio, 0.0), maxOutYRatio);

            //Trace.WriteLine($"X ({dx}) | Y ({dy})");

            //double maxOutRatio = 0.98;
            //// Expand output a bit. Likely not going to get a straight line with Gyro
            //double maxOutXRatio = Math.Min(normX / 1.0, 1.0) * maxOutRatio;
            //double maxOutYRatio = Math.Min(normY / 1.0, 1.0) * maxOutRatio;

            //xratio = Math.Min(Math.Max(xratio, 0.0), maxOutXRatio);
            //yratio = Math.Min(Math.Max(yratio, 0.0), maxOutYRatio);

            // QuadOut
            //xratio = 1.0 - ((1.0 - xratio) * (1.0 - xratio));
            //yratio = 1.0 - ((1.0 - yratio) * (1.0 - yratio));

            double xNorm = 0.0, yNorm = 0.0;
            if (xratio != 0.0)
            {
                xNorm = (1.0 - antiX) * xratio + antiX;
            }

            if (yratio != 0.0)
            {
                yNorm = (1.0 - antiY) * yratio + antiY;
            }

            this.xNorm = xNorm * signX;
            this.yNorm = yNorm * signY;

            //Trace.WriteLine($"OutX ({xratio}) | OutY ({yratio})");
            //short axisXOut = (short)filterX.Filter(xNorm * maxDirX,
            //    1.0 / currentRate);
            //short axisYOut = (short)filterY.Filter(yNorm * maxDirY,
            //    1.0 / currentRate);

            ////Trace.WriteLine($"OutX ({axisXOut}) | OutY ({axisYOut})");

            //xbox.RightThumbX = axisXOut;
            //xbox.RightThumbY = axisYOut;
        }

        public override void SoftCopyFromParent(TouchpadMapAction parentAction)
        {
            if (parentAction is TouchpadMouseJoystick tempMouseJoyAction)
            {
                base.SoftCopyFromParent(parentAction);

                this.parentAction = parentAction;
                mappingId = tempMouseJoyAction.mappingId;

                this.touchpadDefinition = new TouchpadDefinition(tempMouseJoyAction.touchpadDefinition);

                // Determine the set with properties that should inherit
                // from the parent action
                IEnumerable<string> useParentProList =
                    fullPropertySet.Except(changedProperties);

                foreach (string parentPropType in useParentProList)
                {
                    switch(parentPropType)
                    {
                        case PropertyKeyStrings.NAME:
                            name = tempMouseJoyAction.name;
                            break;
                        //case PropertyKeyStrings.DEAD_ZONE:
                        //    deadMod.DeadZone = tempStickAction.deadMod.DeadZone;
                        //    break;
                        //case PropertyKeyStrings.MAX_ZONE:
                        //    deadMod.MaxZone = tempStickAction.deadMod.MaxZone;
                        //    break;
                        //case PropertyKeyStrings.ANTIDEAD_ZONE:
                        //    deadMod.AntiDeadZone = tempStickAction.deadMod.AntiDeadZone;
                        //    break;
                        case PropertyKeyStrings.OUTPUT_STICK:
                            outputAction.StickCode = tempMouseJoyAction.outputAction.StickCode;
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}