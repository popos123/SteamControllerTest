﻿using SteamControllerTest.ActionUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.GyroActions
{
    public class GyroNoMapAction : GyroMapAction
    {
        public GyroNoMapAction()
        {
        }

        public GyroNoMapAction(GyroNoMapAction parentAction)
        {
            this.parentAction = parentAction;
            parentAction.hasLayeredAction = true;
            mappingId = parentAction.mappingId;
        }

        public override void BlankEvent(Mapper mapper)
        {
        }

        public override void Event(Mapper mapper)
        {
        }

        public override void Prepare(Mapper mapper, ref GyroEventFrame gyroFrame, bool alterState = true)
        {
        }

        public override void Release(Mapper mapper, bool resetState = true)
        {
        }

        public override GyroMapAction DuplicateAction()
        {
            return new GyroNoMapAction(this);
        }

        public override void SoftCopyFromParent(GyroMapAction parentAction)
        {
            if (parentAction is GyroNoMapAction tempNoAction)
            {
                base.SoftCopyFromParent(parentAction);

                this.parentAction = parentAction;
                mappingId = tempNoAction.mappingId;
            }
        }
    }
}