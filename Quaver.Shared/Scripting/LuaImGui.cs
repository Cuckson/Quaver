using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Text;
using MoonSharp.Interpreter;
using Wobble;
using Wobble.Graphics.ImGUI;
using Wobble.Logging;

namespace Quaver.Shared.Scripting
{
    public class LuaImGui : SpriteImGui
    {
        /// <summary>
        /// </summary>
        private Script WorkingScript { get; set; }

        /// <summary>
        /// </summary>
        private string FilePath { get; }

        /// <summary>
        /// </summary>
        private bool IsResource { get; }

        /// <summary>
        /// </summary>
        private string ScriptText { get; set; }

        /// <summary>
        /// </summary>
        private FileSystemWatcher Watcher { get; }

        /// <summary>
        /// </summary>
        private LuaPluginState State { get; } = new LuaPluginState();

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="isResource"></param>
        public LuaImGui(string filePath, bool isResource = false)
        {
            FilePath = filePath;
            IsResource = isResource;

            UserData.RegisterAssembly(Assembly.GetCallingAssembly());
            RegisterAllVectors();

            LoadScript();

            if (IsResource)
                return;

            Watcher = new FileSystemWatcher(Path.GetDirectoryName(filePath))
            {
                Filter = Path.GetFileName(filePath)
            };

            Watcher.Changed += OnFileChanged;
            Watcher.Created += OnFileChanged;
            Watcher.Deleted += OnFileChanged;

            // Begin watching.
            Watcher.EnableRaisingEvents = true;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public override void Destroy()
        {
            Watcher?.Dispose();
            base.Destroy();
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        protected override void RenderImguiLayout()
        {
            try
            {
                State.DeltaTime = GameBase.Game.TimeSinceLastFrame;
                WorkingScript.Call(WorkingScript.Globals["draw"]);
            }
            catch (Exception e)
            {
                Logger.Error(e, LogType.Runtime);
            }
        }

        /// <summary>
        ///     Loads the text from the script
        /// </summary>
        private void LoadScript()
        {
            WorkingScript = new Script(CoreModules.Preset_HardSandbox);

            try
            {
                if (IsResource)
                {
                    var buffer = GameBase.Game.Resources.Get(FilePath);
                    ScriptText = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                }
                else
                {
                    ScriptText = File.ReadAllText(FilePath);
                }

                WorkingScript.DoString(ScriptText);
            }
            catch (Exception e)
            {
                Logger.Error(e, LogType.Runtime);
            }

            WorkingScript.Globals["imgui"] = typeof(ImGuiWrapper);
            WorkingScript.Globals["state"] = State;
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            LoadScript();
            Logger.Important($"Script: {FilePath} has been loaded", LogType.Runtime);
        }

        /// <summary>
        ///     Handles registering the Vector types for the script
        /// </summary>
        private void RegisterAllVectors() {

            // Vector 2
            Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.Table, typeof(Vector2),
                dynVal => {
                    var table = dynVal.Table;
                    var x = (float)(double)table[1];
                    var y = (float)(double)table[2];
                    return new Vector2(x, y);
                }
            );

            Script.GlobalOptions.CustomConverters.SetClrToScriptCustomConversion<Vector2>(
                (script, vector) => {
                    var x = DynValue.NewNumber(vector.X);
                    var y = DynValue.NewNumber(vector.Y);
                    var dynVal = DynValue.NewTable(script, x, y);
                    return dynVal;
                }
            );

            // Vector3
            Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.Table, typeof(Vector3),
                dynVal => {
                    var table = dynVal.Table;
                    var x = (float)((double)table[1]);
                    var y = (float)((double)table[2]);
                    var z = (float)((double)table[3]);
                    return new Vector3(x, y, z);
                }
            );

            Script.GlobalOptions.CustomConverters.SetClrToScriptCustomConversion<Vector3>(
                (script, vector) => {
                    var x = DynValue.NewNumber(vector.X);
                    var y = DynValue.NewNumber(vector.Y);
                    var z = DynValue.NewNumber(vector.Z);
                    var dynVal = DynValue.NewTable(script, x, y, z);
                    return dynVal;
                }
            );
        }
    }
}