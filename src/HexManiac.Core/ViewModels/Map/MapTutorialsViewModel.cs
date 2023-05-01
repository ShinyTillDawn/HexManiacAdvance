﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace HavenSoft.HexManiac.Core.ViewModels.Map {
   public enum Tutorial {
      MiddleClick_PanMap,
      ArrowKeys_PanMap,
      Wheel_ZoomMap,

      LeftClickBlock_SelectBlock,
      LeftClickMap_DrawBlock,
      ToolbarUndo_Undo,
      DoubleClick_PaintBlock,
      ControlClick_FillRect,
      EscapeKey_UnselectBlock,
      DragMap_SwapBlock,
      DragBlocks_SelectBlocks,

      BlockButton_EditBlocks,
      ClickTile_SelectTile,
      ClickBlock_DrawTile,
      FlipButton_FlipBlock,

      RightClickMap_SelectBlock,
      RightDragMap_SelectBlocks,

      LeftClick_SelectEvent,
      DoubleClick_FollowWarp,
      BackButton_GoBack,
      DragEvent_MoveEvent,
      ClickMap_UnselectEvent,
      DoubleClickEvent_SeeScript,
      EventButtons_CycleEvent,

      DragConnectionButtons_ResizeMap,
      RightClick_CreateConnection,
      DragButtons_AdjustConnection,

      ToolbarButton_EditBorderBlock,
      ToolbarButton_GotoWildData,
      ToolbarButton_EditMapHeader,
      ToolbarTemplate_CreateObject,
      ToolbarTemplate_CreateEvent,
      RightClick_WarpNewMap,
      ToolbarTemplate_ConfigureObject,

      SpaceBar_ShowBeneath,
      Ctrl_HideEvents,
   }

   public class MapTutorialsViewModel : ViewModelCore {
      public const int VisibleCount = 5;
      public const string
         LeftClick = "LeftMouseButton",
         MiddleClick = "MiddleMouseButton",
         RightClick = "RightMouseButton";
      public ObservableCollection<MapTutorialViewModel> Tutorials { get; } = new();

      public string CompletionPercent {
         get {
            double completed = Tutorials.Sum(tut => tut.Incomplete ? 0 : 1);
            return $"{completed/Tutorials.Count:p}";
         }
      }

      public bool SeeMore => Tutorials.Count(tut => tut.Incomplete) > VisibleCount;

      public event EventHandler CompletedTutorial;

      public MapTutorialsViewModel() {
         // general navigation
         {
            Tutorials.Add(new(MiddleClick, "Move Map", "Middle-Click and drag to move the maps around on the screen."));
            Tutorials.Add(new("ArrowKeys", "Move Map", "Use the arrow keys to move the maps around on the screen."));
            Tutorials.Add(new(MiddleClick, "Zoom Map", "Use the scroll wheel to zoom in and out of the map. Zooming in will hide the border blocks."));
         }

         // select block / draw / paint
         {
            Tutorials.Add(new(LeftClick, "Select Block", "Click a block in the Blocks panel to select it."));
            Tutorials.Add(new(LeftClick, "Draw Block", "Click/Drag over the map to draw with the selected block."));
            Tutorials.Add(new("UndoArrow", "Undo", "Use the Undo button in the toolbar to undo any mistakes."));
            Tutorials.Add(new("FillBucket", "Paint Blocks", "Double-Click over the map to paint an area with the selected block."));
            Tutorials.Add(new("CtrlKey", "Fill Rectangle", "Hold Control, then left-click and drag over an area to create a rectangle of blocks."));
            Tutorials.Add(new("EscapeKey", "Unselect Block", "Press the escape key to unselect a block."));
            Tutorials.Add(new(LeftClick, "Move Block", "Click/Drag over the map with no selected block to move the blocks on the map."));
            Tutorials.Add(new(LeftClick, "Select Blocks", "Left-Click and drag on the Blocks panel to select multiple blocks. When multiple blocks are selected, you can draw, but not double-click fill."));
         }

         // editing blocks
         {
            Tutorials.Add(new("RightAngleArrow", "Edit Block", "Blocks are made of Tiles in a 2x2 grid, with foreground and background layers. Select a block, then click 'Edit Block' in the Blocks panel to show the Tiles panel."));
            Tutorials.Add(new(LeftClick, "Select Tile", "Click a tile in the Tiles panel to select it."));
            Tutorials.Add(new(LeftClick, "Draw Tile", "Click on a block's foreground or background tile at the top of the Blocks panel to replace it with your selected tile/palette."));
            Tutorials.Add(new("ArrowsLeftRight", "Flip Tile", "Click the arrows next to the selected block's foreground/background tile to flip that tile vertically or horizontally."));
         }

         // right-click map
         {
            Tutorials.Add(new(RightClick, "Select Block", "Right-Click the map to select that block."));
            Tutorials.Add(new(RightClick, "Select Blocks", "Right-Click and drag on the map to select multiple blocks. When multiple blocks are selected, you can draw, but not paint."));
         }

         // editing events
         {
            Tutorials.Add(new(LeftClick, "Select Event", "Click an event (character, blue warp, green script, or red signpost) in the map to select it. Selecting an event will unselect any active blocks."));
            Tutorials.Add(new("LeftMouseDoubleClick", "Follow Warp", "Double-Click on a warp event to go to the map it references."));
            Tutorials.Add(new("LeftArrow", "Go Back", "Use the back arrow in the toolbar to return to the previous map or data."));
            Tutorials.Add(new("FourDirectionArrows", "Move Event", "Drag an event to move it on the map."));
            Tutorials.Add(new(LeftClick, "Unselect Event", "Click on any part of the map to unselect the event."));
            Tutorials.Add(new("LeftMouseDoubleClick", "See Script", "Double-Click on an object, script tile, or signpost to jump to its script in the other tab."));
            Tutorials.Add(new("ArrowsLeftRight", "Cycle Events", $"With an event selected, use the Group buttons at the top of the Events panel to change between the 5 groups of events:{Environment.NewLine}Objects, Warps, Scripts, Sigpost, and Fly."));
         }

         // connections
         {
            Tutorials.Add(new("ArrowsLeftRight", "Resize Map", "Use the arrows on the four edges of the map to resize the map."));
            Tutorials.Add(new(RightClick, "New Connection", "Right-click an edge arrow to connect a new or existing map."));
            Tutorials.Add(new("ArrowsLeftRight", "Edit Connection", "Use the arrows on the corners of the map to adjust the offset between the two maps."));
         }

         // other toolbar options
         {
            Tutorials.Add(new("RightAngleArrow", "Edit Border Block", "Each map has a 'border block' that shows for anything onscreen that's out of bounds of the map. Click the button in the toolbar to edit it."));
            Tutorials.Add(new("OutAngleArrow", "Goto Wild Data", "Click this button in the toolbar to create or edit the wild pokemon that appear in the grass, when surfing, when breaking trees/rocks, or when fishing."));
            Tutorials.Add(new("LeftAngleArrow", "Edit Map Header", "Each map has additional information, such as its music and weather. Click the button in the toolbar to edit it."));
            Tutorials.Add(new("Selection", "Create Object", "Drag the character sprite from the top toolbar to create object events."));
            Tutorials.Add(new("Selection", "Create Event", "Drag the Blue/Green/Red squares from the top toolbar to create warp events, script events, and signpost events."));
            Tutorials.Add(new(RightClick, "Warp New Map", "Right-Click a warp to create a new map to warp to."));
            Tutorials.Add(new("Settings", "Configure Object", "Click the gear icon next to the character sprite to choose what type of object you want to add."));
         }

         // additions
         {
            Tutorials.Add(new("Remove", "Show Beneath", "Hold Spacebar to see the map from the Dive/Emerge connection. Double-click to switch to that map."));
            Tutorials.Add(new("CtrlKey", "Hide Events", "Hold Control to hide events. You can right-click to select a block under an event."));
         }

         Reset();
         for(int i = 0; i < Tutorials.Count; i++) {
            Tutorials[i].RequestClose += (sender, e) => CompleteNoCheck((Tutorial)Tutorials.IndexOf((MapTutorialViewModel)sender));
            Tutorials[i].Index = i + 1;
         }
      }

      public void Reset() {
         for (int i = 0; i < Tutorials.Count; i++) {
            Tutorials[i].Incomplete = true;
            Tutorials[i].TargetPosition = i;
         }
      }

      public void Load(string[] metadata) {
         var line = metadata.FirstOrDefault(str => str.StartsWith("MapTutorials = '''"));
         if (line == null) return;
         var content = line.Split("'''")[1];
         for (int i = 0; i < Tutorials.Count; i++) {
            if (i >= content.Length || content[i] != '1') continue;
            Complete((Tutorial)i);
         }
      }

      public string Serialize() {
         var tuts = new char[Tutorials.Count];
         for (int i = 0; i < Tutorials.Count; i++) tuts[i] = Tutorials[i].Incomplete ? '0' : '1';
         return $"MapTutorials = '''{new string(tuts)}'''";
      }

      public void DismissAll() {
         for (int i = 0; i < Tutorials.Count; i++) {
            if (!Tutorials[i].Incomplete) continue;
            Tutorials[i].Incomplete = false;
            Tutorials[i].TriggerAnimation();
         }
         NotifyPropertyChanged(nameof(CompletionPercent));
      }

      public void Complete(Tutorial id) {
         var index = (int)id;
         if (!Tutorials[index].Incomplete) return;
         CompleteNoCheck(id);
         CompletedTutorial.Raise(Tutorials[index]);
      }

      private void CompleteNoCheck(Tutorial id) {
         var index = (int)id;
         if (!Tutorials[index].Incomplete) return;
         Tutorials[index].Incomplete = false;
         for (int i = index + 1; i < Tutorials.Count; i++) {
            Tutorials[i].TargetPosition -= 1;
            if (Tutorials[i].Incomplete && Tutorials[i].TargetPosition < VisibleCount) Tutorials[i].TriggerAnimation();
         }
         NotifyPropertiesChanged(nameof(CompletionPercent), nameof(SeeMore));
      }
   }

   public record MapTutorialViewModel(string Icon, string Title, string Content) : INotifyPropertyChanged {
      public event EventHandler AnimateMovement, RequestClose;
      public event PropertyChangedEventHandler? PropertyChanged;

      private bool incomplete;
      private double target;

      public bool Incomplete { get => incomplete; set => PropertyChanged.TryUpdate(this, ref incomplete, value); }
      public double TargetPosition { get => target; set => PropertyChanged.TryUpdate(this, ref target, value); }
      public double TopEdge => TargetPosition * 90;
      public int Index { get; set; }
      public string IndexText => $"{Index} of 36";

      public void TriggerAnimation() => AnimateMovement.Raise(this);

      public void Close() => RequestClose.Raise(this);
   }
}
