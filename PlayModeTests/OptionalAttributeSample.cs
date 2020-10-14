// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough 

namespace Nementic.Validation
{
    using UnityEngine;

    internal sealed class OptionalAttributeSample : MonoBehaviour
    {
#pragma warning disable 0649

        // This field is optional and therefore ignored by the validation system.
        [Optional]
        public GameObject optionalReference;

        // This is not optional and will log an error if not set in via the inspector.
        public GameObject requiredReference;

        // This field is ignored by the validation system but serves as false positive test case.
        public float[] floatField;

        public NestedClass nestedClass;

        [System.Serializable]
        public class NestedClass
        {
            public GameObject reference;
            public OptionalAttributeSample recursive;
        }

#pragma warning restore 0649
    }
}
