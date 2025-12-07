using SDG.Unturned;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnturnedImages.Module.Helpers;

namespace UnturnedImages.Module.Images
{
    public class CustomVehicleTool : MonoBehaviour
    {
        public class CustomVehicleIconInfo
        {
            public VehicleAsset VehicleAsset { get; }

            public string OutputPath { get; }

            public int Width { get; }

            public int Height { get; }

            public Vector3 Angles { get; }

            public CustomVehicleIconInfo(VehicleAsset vehicleAsset, string outputPath, int width, int height,
                Vector3 angles)
            {
                VehicleAsset = vehicleAsset;
                OutputPath = outputPath;
                Width = width;
                Height = height;
                Angles = angles;
            }
        }

        private static CustomVehicleTool? _instance;
        private static readonly string[] _weirdLookingObjects = new[]
        {
            "DepthMask"
        };

        private readonly Queue<CustomVehicleIconInfo> Icons = new();
        private Transform _camera = null!;

        public static void Load()
        {
            _instance = UnturnedImagesModule.Instance!.GameObject!.AddComponent<CustomVehicleTool>();
        }

        public static void Unload()
        {
            Destroy(_instance);

            _instance = null;
        }

        private void Start()
        {
            var camera = new GameObject();
            _camera = Instantiate(camera).transform;
        }

        public static Transform? GetVehicle(VehicleAsset vehicleAsset)
        {
            var gameObject = vehicleAsset.GetOrLoadModel();

            if (gameObject == null)
            {
                return null;
            }

            return Instantiate(gameObject).transform;
        }

        public static void QueueVehicleIcon(VehicleAsset vehicleAsset, string outputPath, int width, int height,
            Vector3? vehicleAngles = null)
        {
            if (_instance == null)
            {
                return;
            }

            vehicleAngles ??= Vector3.zero;

            var vehicleIconInfo = new CustomVehicleIconInfo(vehicleAsset, outputPath, width, height, vehicleAngles.Value);

            _instance.Icons.Enqueue(vehicleIconInfo);
        }

        private void Update()
        {
            if (Icons.Count == 0)
            {
                return;
            }

            var vehicleIconInfo = Icons.Dequeue();
            var vehicleAsset = vehicleIconInfo.VehicleAsset;

            // Check if this vehicle should be skipped
            if (CrashRecoveryHelper.ShouldSkip(vehicleAsset.GUID))
            {
                UnturnedLog.info($"Skipping {vehicleAsset.GUID} ({vehicleAsset.vehicleName}) - in skip list");
                return;
            }

            // Mark that we're starting to process this vehicle (for crash recovery)
            CrashRecoveryHelper.MarkProcessingStart(vehicleAsset.GUID, vehicleAsset.vehicleName, "vehicle");

            Transform? vehicle = null;
            try
            {
                vehicle = GetVehicle(vehicleAsset);
            }
            catch (Exception ex)
            {
                UnturnedLog.error($"Exception loading model for vehicle {vehicleAsset.GUID} ({vehicleAsset.vehicleName}): {ex.Message}");
                CrashRecoveryHelper.AddToSkipList(vehicleAsset.GUID, vehicleAsset.vehicleName);
                CrashRecoveryHelper.MarkProcessingComplete();
                return;
            }

            if (vehicle == null)
            {
                UnturnedLog.error($"Could not get model for vehicle with ID {vehicleAsset.GUID}");
                CrashRecoveryHelper.MarkProcessingComplete();
                return;
            }

            UnturnedLog.info($"Capturing icon for vehicle with ID {vehicleAsset.GUID} ({vehicleAsset.vehicleName})");

            Transform? vehicleParent = null;
            try
            {
                Layerer.relayer(vehicle, LayerMasks.VEHICLE);
                foreach (var weirdLookingObject in _weirdLookingObjects)
                {
                    var child = vehicle.Find(weirdLookingObject);
                    if (child != null)
                    {
                        child.gameObject.SetActive(false);
                    }
                }

                // fix rotors
                var rotors = vehicle.Find("Rotors");
                if (rotors != null)
                {
                    for (var i = 0; i < rotors.childCount; i++)
                    {
                        var rotor = rotors.GetChild(i);

                        var model0 = rotor.Find("Model_0");
                        var model1 = rotor.Find("Model_1");

                        // Skip if rotor models are missing
                        if (model0 == null || model1 == null)
                            continue;

                        var renderer0 = model0.GetComponent<Renderer>();
                        var renderer1 = model1.GetComponent<Renderer>();

                        if (renderer0 == null || renderer1 == null)
                            continue;

                        var material0 = renderer0.material;
                        var material1 = renderer1.material;

                        if (vehicleAsset.requiredShaderUpgrade)
                        {
                            if (StandardShaderUtils.isMaterialUsingStandardShader(material0))
                            {
                                StandardShaderUtils.setModeToTransparent(material0);
                            }
                            if (StandardShaderUtils.isMaterialUsingStandardShader(material1))
                            {
                                StandardShaderUtils.setModeToTransparent(material1);
                            }
                        }

                        var color = material0.color;
                        color.a = 1f;
                        material0.color = color;

                        color.a = 0f;
                        material1.color = color;

                        rotor.localRotation = Quaternion.identity;
                    }
                }

                vehicleParent = new GameObject().transform;
                vehicle.SetParent(vehicleParent);

                vehicleParent.position = new Vector3(-256f, -256f, 0f);

                if (_camera == null)
                    _camera = Instantiate(new GameObject()).transform;

                _camera.SetParent(vehicle, false);

                vehicle.Rotate(vehicleIconInfo.Angles);
                _camera.rotation = Quaternion.identity;

                var orthographicSize = CustomImageTool.CalculateOrthographicSize(vehicleAsset, vehicleParent.gameObject,
                    _camera, vehicleIconInfo.Width, vehicleIconInfo.Height, out var cameraPosition);

                _camera.position = cameraPosition;

                if (!vehicleAsset.SupportsPaintColor)
                {
                    Texture2D texture = CustomImageTool.CaptureIcon(vehicleAsset.GUID, 0, vehicle, _camera,
                    vehicleIconInfo.Width, vehicleIconInfo.Height, orthographicSize, true);

                    var path = $"{vehicleIconInfo.OutputPath}.png";

                    var bytes = texture.EncodeToPNG();

                    ReadWrite.writeBytes(path, false, false, bytes);
                }
                else
                {
                    var color32 = vehicleAsset.GetRandomDefaultPaintColor();
                    Color color = color32.HasValue ? color32.Value : Color.red;
                    PaintableVehicleSection[] paintableVehicleSections = vehicleAsset.PaintableVehicleSections;
                    for (int i = 0; i < paintableVehicleSections.Length; i++)
                    {
                        PaintableVehicleSection paintableVehicleSection = paintableVehicleSections[i];
                        Transform transform = vehicle.Find(paintableVehicleSection.path);
                        if (transform == null)
                        {
                            Assets.reportError(vehicleAsset, "paintable section missing transform \"" + paintableVehicleSection.path + "\"");
                            continue;
                        }

                        Renderer component = transform.GetComponent<Renderer>();
                        if (component == null)
                        {
                            Assets.reportError(vehicleAsset, "paintable section missing renderer \"" + paintableVehicleSection.path + "\"");
                            continue;
                        }

                        component.material.SetColor(Shader.PropertyToID("_PaintColor"), color);
                    }

                    Texture2D texture = CustomImageTool.CaptureIcon(vehicleAsset.GUID, 0, vehicle, _camera,
                            vehicleIconInfo.Width, vehicleIconInfo.Height, orthographicSize, true);

                    var path = $"{vehicleIconInfo.OutputPath}.png";

                    var bytes = texture.EncodeToPNG();

                    ReadWrite.writeBytes(path, false, false, bytes);
                }

                _camera.SetParent(null);
            }
            catch (Exception ex)
            {
                UnturnedLog.error($"Exception while capturing icon for vehicle {vehicleAsset.GUID} ({vehicleAsset.vehicleName}): {ex.Message}");
                UnturnedLog.error(ex.StackTrace ?? "No stack trace available");
                CrashRecoveryHelper.AddToSkipList(vehicleAsset.GUID, vehicleAsset.vehicleName);

                // Cleanup camera if needed
                if (_camera != null)
                {
                    _camera.SetParent(null);
                }
            }
            finally
            {
                // Always cleanup and mark complete
                if (vehicleParent != null)
                {
                    Destroy(vehicleParent.gameObject);
                }
                CrashRecoveryHelper.MarkProcessingComplete();
            }
        }
    }
}
