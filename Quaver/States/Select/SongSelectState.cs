﻿using System;
using Microsoft.Xna.Framework;
using Quaver.API.Enums;
using Quaver.Audio;
using Quaver.Config;
using Quaver.Discord;
using Quaver.GameState;
using Quaver.Graphics.Buttons;
using Quaver.Graphics.Enums;
using Quaver.Graphics.Sprites;
using Quaver.Graphics.UserInterface;
using Quaver.Input;
using Quaver.Logging;
using Quaver.Main;
using Quaver.Modifiers;
using Quaver.States.Enums;
using Quaver.States.Loading.Map;
using Quaver.States.Menu;

namespace Quaver.States.Select
{
    internal class SongSelectState : IGameState
    {
        /// <summary>
        ///     The current state
        /// </summary>
        public State CurrentState { get; set; } = State.MainMenu;

        /// <summary>
        ///     Update Ready?
        /// </summary>
        public bool UpdateReady { get; set; }

        /// <summary>
        ///     The QuaverUserInterface that controls and displays map selection
        /// </summary>
        private MapSelectSystem MapOrganizer { get; set; }

        /// <summary>
        ///     QuaverContainer
        /// </summary>
        private QuaverContainer QuaverContainer { get; set; } = new QuaverContainer();

        /// <summary>
        ///     Reference to the play button
        /// </summary>        
        private QuaverTextButton PlayButton { get; set; }

        /// <summary>
        ///     Reference to the back button
        /// </summary>        
        private QuaverTextButton BackButton { get; set; }

        /// <summary>
        ///     Reference to the speed gameplayModifier button
        /// </summary>
        private QuaverTextButton SpeedModButton { get; set; }

        /// <summary>
        ///     Reference to the toggle pitch button
        /// </summary>
        private QuaverTextButton TogglePitch { get; set; }

        /// <summary>MapOrganizer
        ///     Position of mouse from previous frame
        /// </summary>
        private float PreviousMouseYPosition { get; set; }

        /// <summary>
        ///     Current Input Manager for this state
        /// </summary>
        private SongSelectInputManager SongSelectInputManager { get; set; }

        /// <summary>
        ///     Determines how much time has passed since initiation
        /// </summary>
        private float TimeElapsedSinceStartup { get; set; }

        /// <summary>
        ///     Stops the Map Organizer from scrolling too fast on high framerate
        /// </summary>
        private float KeyboardScrollBuffer { get; set; }

        /// <summary>
        ///     Initialize
        /// </summary>
        public void Initialize()
        {
            GameBase.GameWindow.Title = "Quaver";

            //Initialize Helpers
            MapOrganizer = new MapSelectSystem();
            MapOrganizer.Initialize(this);
            SongSelectInputManager = new SongSelectInputManager();

            // Update Discord Presence
            DiscordController.ChangeDiscordPresence("Song Select", "In the menus");

            // Initalize buttons
            CreatePlayMapButton();
            CreateBackButton();
            CreateSpeedModButton();
            CreateTogglePitchButton();

            // Update overlay
            GameBase.GameOverlay.OverlayActive = true;

            //Add map selected text TODO: remove later
            try
            {
                Logger.Add("MapSelected", "Map Selected: " + GameBase.SelectedMap.Artist + " - " + GameBase.SelectedMap.Title + " [" + GameBase.SelectedMap.DifficultyName + "]", Color.Yellow);
            }
            catch (Exception e)
            {
                Logger.LogError(e, LogType.Runtime);
            }

            UpdateReady = true;
        }

        /// <summary>
        ///     Unload
        /// </summary>
        public void UnloadContent()
        {
            Logger.Remove("MapSelected");

            UpdateReady = false;
            PlayButton.Clicked -= OnPlayMapButtonClick;
            BackButton.Clicked -= OnBackButtonClick;
            SpeedModButton.Clicked -= OnSpeedModButtonClick;
            TogglePitch.Clicked -= OnTogglePitchButtonClick;
            MapOrganizer.UnloadContent();
            QuaverContainer.Destroy();
        }

        /// <summary>
        ///     Update
        /// </summary>
        public void Update(double dt)
        {
            //Check input to update song select ui
            TimeElapsedSinceStartup += (float)dt;
            KeyboardScrollBuffer += (float)dt;

            // It will ignore input until 250ms go by
            if (!MapOrganizer.ScrollingDisabled && TimeElapsedSinceStartup > 250)
            {
                SongSelectInputManager.CheckInput();

                // Check and update any mouse input
                if (SongSelectInputManager.RightMouseIsDown)
                    MapOrganizer.SetMapOrganizerPosition(-SongSelectInputManager.MouseYPos / GameBase.WindowRectangle.Height);
                else if (SongSelectInputManager.LeftMouseIsDown)
                    MapOrganizer.OffsetMapOrganizerPosition(GameBase.MouseState.Position.Y - PreviousMouseYPosition);
                else if (SongSelectInputManager.CurrentScrollAmount != 0)
                    MapOrganizer.OffsetMapOrganizerPosition(SongSelectInputManager.CurrentScrollAmount);

                // Check and update any keyboard input
                int scroll = 0;
                if (SongSelectInputManager.UpArrowIsDown || SongSelectInputManager.LeftArrowIsDown)
                    scroll += 1;
                if (SongSelectInputManager.RightArrowIsDown || SongSelectInputManager.DownArrowIsDown)
                    scroll -= 1;

                if (scroll != 0 && KeyboardScrollBuffer > 100)
                {
                    KeyboardScrollBuffer = 0;
                    if (scroll > 0) ScrollUpMapIndex();
                    else if (scroll < 0) ScrollDownMapIndex();
                }
                PreviousMouseYPosition = SongSelectInputManager.MouseYPos;
            }

            //Update Objects
            QuaverContainer.Update(dt);
            MapOrganizer.Update(dt);

            // Repeat the song preview if necessary
            RepeatSongPreview();
        }

        /// <summary>
        ///     Draw
        /// </summary>
        public void Draw()
        {
            GameBase.SpriteBatch.Begin();
            BackgroundManager.Draw();
            QuaverContainer.Draw();
            MapOrganizer.Draw();

            GameBase.SpriteBatch.End();
        }

        /// <summary>
        ///     Creates and initializes the play button
        /// </summary>
        private void CreatePlayMapButton()
        {
            // Create play button
            PlayButton = new QuaverTextButton(new Vector2(200, 50), "Play Map")
            {
                PosY = 370,
                Alignment = Alignment.TopLeft,
                Parent = QuaverContainer
            };

            PlayButton.Clicked += OnPlayMapButtonClick;
        }

        /// <summary>
        ///     Changes to the song loading state when the play map button is clicked.
        /// </summary>
        private void OnPlayMapButtonClick(object sender, EventArgs e)
        {
            GameBase.LoadedSkin.SoundClick.Play(GameBase.SoundEffectVolume, 0, 0);
            GameBase.GameStateManager.ChangeState(new MapLoadingState());
        }

        private void ScrollUpMapIndex()
        {
            MapOrganizer.OffsetMapOrganizerIndex(-1);
        }

        private void ScrollDownMapIndex()
        {
            MapOrganizer.OffsetMapOrganizerIndex(1);
        }

        /// <summary>
        ///     Responsible for repeating the song preview in song select once the song is over.
        /// </summary>
        private void RepeatSongPreview()
        {
            if (GameBase.AudioEngine.Position < GameBase.AudioEngine.Length || AudioEngine.Stream == 0)
                return;

            // Reload the audio and play at the song preview
            try
            {
                GameBase.AudioEngine.ReloadStream();
                GameBase.AudioEngine.Play(GameBase.SelectedMap.AudioPreviewTime);
            } catch (Exception e)
            {
                Logger.LogError(e, LogType.Runtime);
            }
        }

        /// <summary>
        ///     Creates the back button
        /// </summary>        
        private void CreateBackButton()
        {
            // Create back button
            BackButton = new QuaverTextButton(new Vector2(200, 50), "Back")
            {
                PosY = - 90,
                Alignment = Alignment.BotCenter,
                Parent = QuaverContainer
            };
            BackButton.Clicked += OnBackButtonClick;
        }

        /// <summary>
        ///     Whenever the back button is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBackButtonClick(object sender, EventArgs e)
        {
            GameBase.LoadedSkin.SoundBack.Play(GameBase.SoundEffectVolume, 0, 0);
            GameBase.GameStateManager.ChangeState(new MainMenuState());
        }

        /// <summary>
        ///     Creates the speed gameplayModifier button
        /// </summary>
        private void CreateSpeedModButton()
        {
            // Create ManiaModSpeed Mod QuaverButton
            SpeedModButton = new QuaverTextButton(new Vector2(200, 50), $"Add Speed Mod {GameBase.AudioEngine.PlaybackRate}x")
            {
                PosY = - 120,
                Alignment = Alignment.BotLeft,
                Parent = QuaverContainer
            };
            SpeedModButton.Clicked += OnSpeedModButtonClick;
        }

        /// <summary>
        ///     Adds speed gameplayModifier to game
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSpeedModButtonClick(object sender, EventArgs e)
        {
            try
            {
                // Activate the current speed gameplayModifier depending on the (current game clock + 0.1)
                switch ((float)Math.Round(GameBase.AudioEngine.PlaybackRate + 0.1f, 1))
                {
                    // In this case, 2.1 really means 0.5x, given that we're checking
                    // for the current GameClock + 0.1. If it's 2.1, we reset it back to 0.5x
                    case 2.1f:
                        ModManager.AddMod(ModIdentifier.Speed05X);
                        break;
                    // If it ends up being 1.0x, we'll just go ahead and remove all the mods.
                    case 1.0f:
                        ModManager.RemoveSpeedMods();
                        break;
                    case 0.6f:
                        ModManager.AddMod(ModIdentifier.Speed06X);
                        break;
                    case 0.7f:
                        ModManager.AddMod(ModIdentifier.Speed07X);
                        break;
                    case 0.8f:
                        ModManager.AddMod(ModIdentifier.Speed08X);
                        break;
                    case 0.9f:
                        ModManager.AddMod(ModIdentifier.Speed09X);
                        break;
                    case 1.1f:
                        ModManager.AddMod(ModIdentifier.Speed11X);
                        break;
                    case 1.2f:
                        ModManager.AddMod(ModIdentifier.Speed12X);
                        break;
                    case 1.3f:
                        ModManager.AddMod(ModIdentifier.Speed13X);
                        break;
                    case 1.4f:
                        ModManager.AddMod(ModIdentifier.Speed14X);
                        break;
                    case 1.5f:
                        ModManager.AddMod(ModIdentifier.Speed15X);
                        break;
                    case 1.6f:
                        ModManager.AddMod(ModIdentifier.Speed16X);
                        break;
                    case 1.7f:
                        ModManager.AddMod(ModIdentifier.Speed17X);
                        break;
                    case 1.8f:
                        ModManager.AddMod(ModIdentifier.Speed18X);
                        break;
                    case 1.9f:
                        ModManager.AddMod(ModIdentifier.Speed19X);
                        break;
                    case 2.0f:
                        ModManager.AddMod(ModIdentifier.Speed20X);
                        break;
                }
            }
            catch (Exception ex)
            {
                ModManager.RemoveSpeedMods();
            }

            // Change the song speed directly.
            SpeedModButton.QuaverTextSprite.Text = $"Add Speed Mod {GameBase.AudioEngine.PlaybackRate}x";
        }

        /// <summary>
        ///     Creates the toggle pitch button
        /// </summary>
        private void CreateTogglePitchButton()
        {
            TogglePitch = new QuaverTextButton(new Vector2(200, 50), $"Toggle Pitch: {ConfigManager.Pitched}")
            {
                Alignment = Alignment.TopLeft,
                PosY = 430,
                Parent = QuaverContainer
            };
            TogglePitch.Clicked += OnTogglePitchButtonClick;
        }

        /// <summary>
        ///     Toggles pitching for speed modifications
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTogglePitchButtonClick(object sender, EventArgs e)
        {
            GameBase.AudioEngine.TogglePitch();
            TogglePitch.QuaverTextSprite.Text = $"Toggle Pitch: {ConfigManager.Pitched}";
        }
    }
}
