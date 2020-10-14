// Copyright (c) 2020 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough 

namespace Nementic.Validation
{
	using NUnit.Framework;
	using System.Collections;
	using UnityEditor;
	using UnityEditor.SceneManagement;
	using UnityEngine.SceneManagement;

	internal class SceneValidationTest
	{
		private SceneValidator sceneValidator;

		[SetUp]
		public void Setup()
		{
			sceneValidator = new SceneValidator();
		}

		[TearDown]
		public void TearDown()
		{
			sceneValidator = null;
		}

		[Test]
		[Category("SceneValidation")]
		[TestCaseSource(nameof(LoadScenes))]
		public void ValidateScene(SceneInfo sceneInfo)
		{
			Scene scene;
			try
			{
				scene = EditorSceneManager.OpenScene(sceneInfo.path);
			}
			catch
			{
				// This can only happen if the TestCaseSource has retrieved
				// a scene path from the AssetDatabase, but then the scene
				// was deleted from disk without the project refreshing.
				scene = new Scene();
				Assert.Inconclusive("Failed to load scene: " + sceneInfo.path);
			}

			ValidationReport report = sceneValidator.CreateValidationReport(scene);
			if (report.HasErrors)
				Assert.Fail(string.Join("\n", report.Errors));
			else
				Assert.Pass();
		}

		public static IEnumerable LoadScenes()
		{
			string[] guids = AssetDatabase.FindAssets("t:scene", new[] { "Assets" });
			for (int i = 0; i < guids.Length; i++)
				yield return new SceneInfo(guids[i]);
		}

		public struct SceneInfo
		{
			public readonly string guid;
			public readonly string path;
			public readonly string displayName;

			public SceneInfo(string guid)
			{
				this.guid = guid;
				this.path = AssetDatabase.GUIDToAssetPath(guid);
				this.displayName = TrimForDisplay(path);
			}

			private static string TrimForDisplay(string path)
			{
				// Remove 'Assets/' from the start and '.unity' from the end.
				int startIndex = 7;
				int length = path.Length - startIndex - 6;
				return path.Substring(startIndex, length);
			}

			public override string ToString()
			{
				return displayName;
			}
		}
	}
}
