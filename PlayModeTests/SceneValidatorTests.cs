// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough 

#if UNITY_EDITOR && NUNIT_ENABLED
namespace Nementic.Validation
{
    using NUnit.Framework;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class SceneValidatorTests
    {
        [Test]
        public void CreateValidationReport_UnsetReference_HasErrorWithMessage()
        {
            OptionalAttributeSample sample = CreateTestObject();

            ValidationReport report = RunValidation();

            Assert.IsTrue(report.HasErrors, "Error report contains errors.");

            report.HandleErrors(error =>
            {
                Assert.IsTrue(error.message.Contains("Missing required reference"));
                Assert.IsTrue(error.message.Contains("TestGameObject"));
            });

            Object.DestroyImmediate(sample.gameObject);
        }

        [Test]
        public void CreateValidationReport_SetReference_HasNoErrors()
        {
            OptionalAttributeSample sample = CreateTestObject();
            sample.requiredReference = new GameObject("MyRequiredReference");

            ValidationReport report = RunValidation();

            Assert.IsFalse(report.HasErrors, "Error report contains errors.");

            Object.DestroyImmediate(sample.requiredReference);
            Object.DestroyImmediate(sample.gameObject);
        }

        [Test]
        public void CreateValidationReport_DestroyedReference_HasErrorWithMessage()
        {
            OptionalAttributeSample sample = CreateTestObject();
            sample.requiredReference = new GameObject("MyRequiredReference");
            Object.DestroyImmediate(sample.requiredReference);

            ValidationReport report = RunValidation();

            Assert.IsTrue(report.HasErrors, "Error report contains errors.");

            report.HandleErrors(error =>
            {
                Assert.IsTrue(error.message.Contains("Missing required reference"), "Message contains 'missing'.");
                Assert.IsTrue(error.message.Contains("TestGameObject"), "Message contains context name.");
            });

            Object.DestroyImmediate(sample.gameObject);
        }

        private static OptionalAttributeSample CreateTestObject()
        {
            var go = new GameObject("TestGameObject");
            return go.AddComponent<OptionalAttributeSample>();
        }

        private static ValidationReport RunValidation()
        {
            var validator = new SceneValidator();
            Scene activeScene = EditorSceneManager.GetActiveScene();
            return validator.CreateValidationReport(activeScene);
        }
    }
}
#endif
