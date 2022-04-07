﻿using SteamControllerTest.ActionUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.ButtonActions
{
    public abstract class ButtonMapAction : MapAction
    {
        public bool activeEvent;
        public bool active;
        public bool analog;
        public abstract double ButtonDistance { get; }

        public abstract void Prepare(Mapper mapper, bool status, bool alterState = true);
        public abstract void PrepareAnalog(Mapper mapper, double axisValue, bool alterState = true);

        public abstract ButtonMapAction DuplicateAction();
        public virtual void SoftCopyFromParent(ButtonMapAction parentAction)
        {
        }
    }
}