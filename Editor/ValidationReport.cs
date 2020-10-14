// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough 

#if UNITY_EDITOR
namespace Nementic.Validation
{
    using System;
    using System.Collections.Generic;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// Used by the validation system to defer the reporting (e.g. logging)
    /// of errors accumulated during a validation run.
    /// </summary>
    public class ValidationReport
    {
		public IEnumerable<Error> Errors => errors;

        private readonly List<Error> errors = new List<Error>();
        private Action<Error> logAction;

        public void AddError(string message, UnityEngine.Object context)
        {
            errors.Add(new Error(message, context));
        }

		public void AddErrorFormatted(string propertyName, string typeName, UnityEngine.MonoBehaviour monoBehaviour, string prefix = "Missing required reference")
		{
			AddError($"{prefix} '{propertyName}' ({typeName}) on '{monoBehaviour.name}'. " +
					 $"Scene: {GetSceneNameOrUnknownLabel(monoBehaviour.gameObject.scene)}", monoBehaviour);
		}

		private static string GetSceneNameOrUnknownLabel(Scene scene)
		{
			return scene.name.Length > 0 ? scene.name : "Unknown";
		}

		public void Clear()
        {
            errors.Clear();
        }

        public bool HasErrors
        {
            get { return errors.Count > 0; }
        }

        public void LogErrors(Severity severityLevel = Severity.Error)
        {
            if (errors.Count == 0)
                return;

            logAction = DefaultErrorHandler;
            HandleErrors(severityLevel);
        }

        private static void DefaultErrorHandler(Error error)
        {
            if (error.severity == Severity.Error)
                UnityEngine.Debug.LogError(error.message, error.context);
            else if (error.severity == Severity.Warning)
                UnityEngine.Debug.LogWarning(error.message, error.context);
            else
                UnityEngine.Debug.Log(error.message, error.context);
        }

        /// <summary>
        /// Calls the error handler of the error's severity is higher
        /// or equal to the provided severityLevel.
        /// </summary>
        public void HandleErrors(Action<Error> errorHandler, Severity severityLevel = Severity.Error)
        {
            for (int i = 0; i < errors.Count; i++)
            {
                if (errors[i].severity >= severityLevel)
                    errorHandler.Invoke(errors[i]);
            }
        }

        public void HandleErrors(Severity severityLevel = Severity.Error)
        {
            for (int i = 0; i < errors.Count; i++)
            {
                if (errors[i].severity >= severityLevel)
                    logAction.Invoke(errors[i]);
            }
        }

        public struct Error
        {
            public readonly string message;
            public readonly UnityEngine.Object context;
            public readonly Severity severity;

            public Error(string message, UnityEngine.Object context, Severity severity = Severity.Error)
            {
                this.message = message;
                this.context = context;
                this.severity = severity;
            }

			public override string ToString()
			{
				return severity.ToString() + ": " + message;
			}
		}

        public enum Severity
        {
            /// <summary>
            /// A validation issue of medium severity.
            /// </summary>
            Warning = 1,

            /// <summary>
            /// A validation issue indicating an error.
            /// </summary>
            Error = 2
        }
    }
} 
#endif