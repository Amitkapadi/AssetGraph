// using UnityEngine;
// using UnityEditor;

// using System;
// using System.Linq;
// using System.IO;
// using System.Collections.Generic;

// namespace AssetGraph {
// 	public class ImporterBase : INodeBase {
// 		public UnityEditor.AssetPostprocessor assetPostprocessor;
// 		public UnityEditor.AssetImporter assetImporter;
// 		public string assetPath;
		
// 		public void Setup (string nodeId, string labelToNext, string package, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
// 			var outputDict = new Dictionary<string, List<InternalAssetData>>();

			
// 			foreach (var groupKey in groupedSources.Keys) {
// 				var inputSources = groupedSources[groupKey];

// 				var assumedImportedAssetDatas = new List<InternalAssetData>();
					
// 				foreach (var inputData in inputSources) {
// 					var assumedImportedBasePath = inputData.absoluteSourcePath.Replace(inputData.sourceBasePath, AssetGraphSettings.IMPORTER_CACHE_PLACE);
// 					var assumedImportedPath = FileController.PathCombine(assumedImportedBasePath, nodeId);
					
// 					var assumedType = AssumeTypeFromExtension();

// 					var newData = InternalAssetData.InternalAssetDataByImporter(
// 						inputData.traceId,
// 						inputData.absoluteSourcePath,
// 						inputData.sourceBasePath,
// 						inputData.fileNameAndExtension,
// 						inputData.pathUnderSourceBase,
// 						assumedImportedPath,
// 						null,
// 						assumedType
// 					);
// 					assumedImportedAssetDatas.Add(newData);
// 				}

// 				outputDict[groupKey] = assumedImportedAssetDatas;
// 			}

// 			Output(nodeId, labelToNext, outputDict, new List<string>());
// 		}
		
// 		public void Run (string nodeId, string labelToNext, string package, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
// 			var usedCache = new List<string>();

// 			var outputDict = new Dictionary<string, List<InternalAssetData>>();

// 			var targetDirectoryPath = FileController.PathCombine(AssetGraphSettings.IMPORTER_CACHE_PLACE, nodeId, GraphStackController.Current_Platform_Package_Folder(package));

// 			foreach (var groupKey in groupedSources.Keys) {
// 				var inputSources = groupedSources[groupKey];

// 				/*
// 					ready import resources from outside of Unity to inside of Unity.
// 				*/
// 				InternalImporter.Attach(this);
// 				foreach (var inputSource in inputSources) {
// 					var absoluteFilePath = inputSource.absoluteSourcePath;
// 					var pathUnderSourceBase = inputSource.pathUnderSourceBase;

// 					var targetFilePath = FileController.PathCombine(targetDirectoryPath, pathUnderSourceBase);

// 					if (GraphStackController.IsCached(inputSource, alreadyCached, targetFilePath)) {
// 						usedCache.Add(targetFilePath);
// 						continue;
// 					}

// 					try {
// 						/*
// 							copy files into local.
// 						*/
// 						FileController.CopyFileFromGlobalToLocal(absoluteFilePath, targetFilePath);
// 					} catch (Exception e) {
// 						Debug.LogError("Importer:" + this + " error:" + e);
// 					}
// 				}
// 				AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
// 				InternalImporter.Detach();
				
// 				// get files, which are already assets.
// 				var localFilePathsAfterImport = FileController.FilePathsInFolder(targetDirectoryPath);

// 				var localFilePathsWithoutTargetDirectoryPath = localFilePathsAfterImport.Select(path => InternalAssetData.GetPathWithoutBasePath(path, targetDirectoryPath)).ToList();
				
// 				var outputSources = new List<InternalAssetData>();

// 				// generate matching between source and imported assets.
// 				foreach (var localFilePathWithoutTargetDirectoryPath in localFilePathsWithoutTargetDirectoryPath) {
// 					foreach (var inputtedSourceCandidate in inputSources) {
// 						var pathsUnderSourceBase = inputtedSourceCandidate.pathUnderSourceBase;

// 						if (localFilePathWithoutTargetDirectoryPath == pathsUnderSourceBase) {
// 							var localFilePathWithTargetDirectoryPath = InternalAssetData.GetPathWithBasePath(localFilePathWithoutTargetDirectoryPath, targetDirectoryPath);

// 							var newInternalAssetData = InternalAssetData.InternalAssetDataByImporter(
// 								inputtedSourceCandidate.traceId,
// 								inputtedSourceCandidate.absoluteSourcePath,// /SOMEWHERE_OUTSIDE_OF_UNITY/~
// 								inputtedSourceCandidate.sourceBasePath,// /SOMEWHERE_OUTSIDE_OF_UNITY/
// 								inputtedSourceCandidate.fileNameAndExtension,// A.png
// 								inputtedSourceCandidate.pathUnderSourceBase,// (Temp/Imported/nodeId/)~
// 								localFilePathWithTargetDirectoryPath,// Assets/~
// 								AssetDatabase.AssetPathToGUID(localFilePathWithTargetDirectoryPath),
// 								AssetGraphInternalFunctions.GetAssetType(localFilePathWithTargetDirectoryPath)
// 							);
// 							outputSources.Add(newInternalAssetData);
// 						}
// 					}
// 				}

// 				/*
// 					check if new Assets are generated, trace it.
// 				*/
// 				var assetPathsWhichAreAlreadyTraced = outputSources.Select(path => path.pathUnderSourceBase).ToList();
// 				var assetPathsWhichAreNotTraced = localFilePathsWithoutTargetDirectoryPath.Except(assetPathsWhichAreAlreadyTraced);
// 				foreach (var newAssetPath in assetPathsWhichAreNotTraced) {
// 					var basePathWithNewAssetPath = InternalAssetData.GetPathWithBasePath(newAssetPath, targetDirectoryPath);
// 					if (alreadyCached.Contains(basePathWithNewAssetPath)) {
// 						var newInternalAssetData = InternalAssetData.InternalAssetDataGeneratedByImporterOrPrefabricator(
// 							basePathWithNewAssetPath,
// 							AssetDatabase.AssetPathToGUID(basePathWithNewAssetPath),
// 							AssetGraphInternalFunctions.GetAssetType(basePathWithNewAssetPath),
// 							false,
// 							false
// 						);
// 						outputSources.Add(newInternalAssetData);
// 					} else {
// 						var newInternalAssetData = InternalAssetData.InternalAssetDataGeneratedByImporterOrPrefabricator(
// 							basePathWithNewAssetPath,
// 							AssetDatabase.AssetPathToGUID(basePathWithNewAssetPath),
// 							AssetGraphInternalFunctions.GetAssetType(basePathWithNewAssetPath),
// 							true,
// 							false
// 						);
// 						outputSources.Add(newInternalAssetData);
// 					}
// 				}

// 				outputDict[groupKey] = outputSources;
// 			}

// 			Output(nodeId, labelToNext, outputDict, usedCache);
// 		}

// 		/*
// 			handled import events.
// 		*/
// 		public virtual void AssetGraphOnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {}
// 		public virtual void AssetGraphOnPostprocessGameObjectWithUserProperties (GameObject g, string[] propNames, object[] values) {}
// 		public virtual void AssetGraphOnPreprocessTexture () {}
// 		public virtual void AssetGraphOnPostprocessTexture (Texture2D texture) {}
// 		public virtual void AssetGraphOnPreprocessAudio () {}
// 		public virtual void AssetGraphOnPostprocessAudio (AudioClip clip) {}
// 		public virtual void AssetGraphOnPreprocessModel () {}
// 		public virtual void AssetGraphOnPostprocessModel (GameObject g) {}
// 		public virtual void AssetGraphOnAssignMaterialModel (Material material, Renderer renderer) {}

// 		public Type AssumeTypeFromExtension () {
// 			return typeof(UnityEngine.Object);
// 		}
// 	}
// }