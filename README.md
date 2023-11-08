# Timeline for Grasshopper

> Timeline is a simple keyframe editor for creating animations in Grasshopper.
![timeline](https://github.com/camnewnham/GH_Timeline/assets/19278856/b2e28fcd-c3c2-4856-b2af-fb0ae01ec776)


## Features

- Double click to record 
- Create animations directly to `.mp4` video files
- Animate multiple components at once
- Animate the camera
- Edit keyframe easing and position with live updates.
- Rhino 6+ Windows and Mac supported
 
![animate_camera](https://github.com/camnewnham/GH_Timeline/assets/19278856/e76dc252-a048-47aa-9be6-c3b55fa18939)

## Usage

1. Add a timeline to your document.
2. Double click the timeline to start tracking changes.
3. Move the time slider and adjust your document and camera to the desired state.
4. Repeat from (2).
5. Double click the timeline to stop tracking.
6. Right click to export your animation.

![animate_slider](https://github.com/camnewnham/GH_Timeline/assets/19278856/c9b099ad-5d2e-403b-80f1-6d04491fc531)

### Usage Notes
- Sort order for the sequences in your timeline is determined by their vertical position in the document.
- Rename your components to update their names on the timeline.
- When dragging the time slider or a keyframe, mouse over another keyframe to snap to the same time.
- The output of the component is a number from 0-1. If you want to interact with other non-supported components (such as moving the camera along a path). This is equivalent to adding an additional number slider and animating with the timeline.

## Supported Components

|       Component        |  Status    |
| ------------- | ---- |
| Main Camera   | ✔️   |
| Number Slider | ✔️   |
| Toggle        | ✔️   |
| MD Slider     | ✔️\* |
| Digit Slider  | ✔️\* |
| Graph Mapper  | ✔️\* |
| Color Swatch  | ✔️\* |
| Gradient      | ✔️\* |
| Color Wheel   | ✔️\* |
| Control Knob  | ✔️\* |
| Panel         | ❌   |
| Color Picker  | ❌   |
| Calendar      | ❌   |
| Clock         | ❌   |

\* Tweening not supported. Implemented via `IGH_StateAwareObject`

If you'd like to see one of these components added or updated to support interpolation, please create an issue.

### Contributing

If you'd like to implement something, please fork and create a pull request, or create an issue to discuss. For ideas, see the [projects page](https://github.com/users/camnewnham/projects/2).
