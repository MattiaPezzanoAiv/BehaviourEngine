﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BehaviourEngine.Interfaces
{
    public interface IState
    {
        void OnStateEnter();
        void OnStateExit();
        IState OnStateUpdate();
    }
}
