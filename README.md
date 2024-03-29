# Unity-Sprite-Sheet-Animation-Extractor
Small Editor Window that automatically creates animations, animator controller and animator override controllers for the 2d sprite sheets.

**Note: It only works with grid-based sprite sheets like these**
![image](https://github.com/kantagara/Unity-Sprite-Sheet-Animation-Extractor/assets/8202013/32183bed-b93d-4990-bcbd-705cee0be14a)

![Demo](/Animation.gif)

First two parameters represent the height and width of the sprite sheet (width also represents number of sprites that will go into the animation)

**Note**: You can have multiple sprite sheets inside of a single .png file but their width and height must match.
Third one is the frame rate i.e. how many animations you want per second.

Common animation data is the foundation of this editor window. It defines the actual states for the state machine and their names. Column Offset represents the actual column the animations you want to extract are on.

Main Animations is meant for dragging the original sprite sheets .png file for which the editor window can create animations and **Animator Controller**.

Additional Animations array is meant for dragging and dropping all the sprite sheets that will re-use the same state machine as the original, but animations will be different. That's why as the end result it will produce **Animator Override Controller**.

Art credits: https://shubibubi.itch.io/cozy-people

