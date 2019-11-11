// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough 

namespace Nementic
{
    using System;
    using UnityEngine;

    /// <summary>
    ///     Used to mark a field to be ignored by the SceneValidation system.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class OptionalAttribute : PropertyAttribute
    {
    }
}