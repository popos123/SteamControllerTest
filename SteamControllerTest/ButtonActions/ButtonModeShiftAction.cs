﻿using SteamControllerTest.ActionUtil;
using SteamControllerTest.ButtonActions;
using SteamControllerTest.MapperUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.ButtonActions
{
    public class ButtonModeShiftAction : ButtonMapAction
    {
        public ButtonMapAction primaryAction;
        public ButtonMapAction modeShiftAction;
        public ButtonMapAction currentAction;
        private ButtonMapAction previousAction;
        private JoypadActionCodes modeShiftTrigger;
        public JoypadActionCodes ModeShiftTrigger
        {
            get => modeShiftTrigger; set => modeShiftTrigger = value;
        }

        public override double ButtonDistance => currentAction.ButtonDistance;

        private bool state;

        public ButtonModeShiftAction()
        {
        }

        public override void Prepare(Mapper mapper, bool status, bool alterState = true)
        {
            state = status;
            bool shifted = false;
            if (state)
            {
                shifted = modeShiftTrigger != JoypadActionCodes.Empty &&
                    mapper.IsButtonActive(modeShiftTrigger);
            }

            if (!shifted)
            {
                currentAction = primaryAction;
            }
            else
            {
                currentAction = modeShiftAction;
            }

            if (alterState)
            {
                stateData.wasActive = state;
                stateData.state = state;
                stateData.axisNormValue = state ? 1.0 : 0.0;
            }
            
            currentAction.Prepare(mapper, status, alterState);
            active = state;
            activeEvent = true;
        }

        public override void PrepareAnalog(Mapper mapper, double axisValue, bool alterState = true)
        {
        }

        public override void Event(Mapper mapper)
        {
            if (previousAction != currentAction && previousAction != null)
            {
                previousAction.Prepare(mapper, false);
                previousAction.Event(mapper);
            }

            currentAction.Event(mapper);

            previousAction = currentAction;
            active = state;
            activeEvent = false;
        }

        public override void Release(Mapper mapper, bool resetState=true)
        {
            if (previousAction != currentAction && previousAction != null)
            {
                previousAction.Release(mapper);
                previousAction = null;
            }

            currentAction?.Release(mapper);
            if (resetState)
            {
                stateData.Reset(true);
            }
        }

        public override ButtonMapAction DuplicateAction()
        {
            throw new NotImplementedException();
        }
    }
}