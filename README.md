# Sphere Ray-cast using Unity
--------------------------

Sphere Ray-casting is a wide-range 3D raycasting method that uses Unity [Physics.SphereCast()](https://docs.unity3d.com/ScriptReference/Physics.SphereCast.html) and [Physics.RayCast()](https://docs.unity3d.com/ScriptReference/Physics.Raycast.html) to detect the best GameObject that can be interacted.

| ![gif](https://i.imgur.com/eSDGxZp.gif) ![gif](https://i.imgur.com/RQWWBCT.gif) |
| 

## Project Description

This project provides scripts needed to implement Sphere Ray-casting and an example scene that shows how it works.

## Table of Content

<!--ts-->
* [Objective](#objective)
* [Performance Overview](#performance-overview)
  * [Analysis of SphereCast](#analysis-of-spherecast)
  * [Analysis of Block Check](#analysis-of-block-check)
* [How to Setup](#how-to-setup)
  * [Requirements](#requirements)
  * [Deployment](#deployment)
* [Example Overview](#example-overview)
  * [Requirement and Deployment](#requirement-and-deployment)
  * [Running the Test](#running-the-test)
* [License](#license)
<!--te-->

## Objective

While in developement of third-person puzzle game Aiku, I was tasked to come up with an improved raycasting method to provide a wider range of detection. Our head developer initially wanted to use the Unity provided SphereCast() method, but there were two missing functionallity in this method that were required to achieve the performance we needed.

One was checking whether the inspect object is blocked or not, whether it may be a terrain or uninteractable objects. This would mean even if the blocked object was scripted to be interactable, it might actually be unsuitable for interaction.

Second was the way SphereCast sorted the objects. Unity3D's Physics.SphereCast() returns an array-based heap of RayCastHit to its provided parameter _hitinfo_, sorted by their distance from the SphereCast's starting position. Although this was a useful information that could be used to prioritize the object to interact with when more than one interactables were in range, our head developer specifically wanted a way of prioritizing object interaction by the angle between the object and center of the screen.

## Performance Overview

Sphere Ray-cast allows wider ray-casting method in first-person game by first gathering all objects inside a range created by sweeping a sphere in front of character and determining if they're blocked by any object.

| ![gif](https://i.imgur.com/pDdh7Q7.gif) | 
|:--:| 
| ***1**Succesful block checks are shown by gizmo lines on editor window. White lines are every objects in-range that are not blocked. Green line indicates the best suitable interactable object. **2**Blue and Pink Cubes are both interactable objects in range of SphereCast(), displayed by red gizmo sphere in editor window. However, when blocked by the uninteractable red wall, it fails block check. (As shown above when cubes are blocked from player's view, white lines are drawn from player to the red wall, but no lines are drawn towards the interactable cubes)* |

There are two versions of sphere ray-cast:

1. DetectInteractableObject.cs:
* This script simply sorts all objects collected by angle from center then returns the closest one to the center that is not blocked.
![gif](https://i.imgur.com/jexAVoq.gif)
2. DetectInteractableObjectComparative.cs:
* This scrit compares the angle between each objects collected and returns the most optimal one. This may not be an object with the smallest angle from the center.
* This script uses greedy algorithm and will have better runtime-complexity than the other one.
* It either returns an object with the closest angle from the camera or closest distance from the player. It also allows a light-weight Observer(Event) patter in designing GameObject interaction.
  * If an object is close enough to center by set angle and is also closest object by distance from player to be so, it becomes an object picked.
  * If no such object was found, the closest object to the center will be returned.
* Check comments on script for more detail.
![gif](https://i.imgur.com/xut6Wj2.gif)

_1 is easier to implement than 2, but 2 has better control and performance._

**Not considering Unity3D's built-in _SphereCast()_ method, 1 Has <img src="https://latex.codecogs.com/gif.latex?O(n^2)" title="O(n^2)" /> worstcase runtime. 2 has <img src="https://latex.codecogs.com/gif.latex?O(n)" title="O(nl)" /> worstcase runtime.**

### Analysis of SphereCast

Both Sphere-Raycasting method uses Physics.SphereCast() to query every objects collided by the sphere sweeped in front of the player. This returns info of all collided object as minimum heap of RayCastHit sorted by distance from player.

**Note that these objects could either be interactable or uninteractable**

```C#
RaycastHit[] allHits; // array-based min-heap containing info of colided objects
allHits = Physics.SphereCastAll(this.transform.position, castRadius,
				this.transform.forward, castDistance); // spherecast to find the objects.
```

 Suppose there were _n_ amounts of objects collided by SphereCast. At worst case, every newly inserted element will be a new minimum in heap, which means it would be the closest object from the play in the heap. If there were _n_ object in the heap prior to the insertion, this would cause the newly inserted minimum to traverse up the height of the binary tree, which would be ![gif](https://latex.codecogs.com/gif.latex?%5Clg%20n). Since every element will be inserted into the heap, performing _n_ number of insert, the worst case runtime of SphereCast is ![gif](https://latex.codecogs.com/gif.latex?O%28n%5Clg%20n%29)

### Analysis of Block Check

Both method of Sphere-RayCasting uses Physics.RayCast() to check if theres anything blocking the object from the player's view. The result is returned as boolean by the function below, and proceeding procedures are only performed if this functionreturns false.

```C#
private bool IsBlocked(GameObject toCheck)
{
    bool toReturn;
    Vector3 dirFromPlayer = toCheck.transform.position - this.transform.position; // acquire directional vector
    RaycastHit hit; //holder for the collided object
    Physics.Raycast(this.transform.position, dirFromPlayer, out hit); // begin raycast from the player camera center
    if (toCheck == hit.collider.gameObject) // check if it hit the object to check
        toReturn = false; // not blocked
    else
        toReturn = true;
    return toReturn; //its blocked
}
```

The function above takes in the object which needs to be block checked as a parameter. It performs a raycast from center of the player's camera towards the object, then check to see if the object first hit by raycast is indeed object passed in as parameter, meaning that there were no other object between player's view and the object.

### Analysis of Angle Comparison

Out of the objects collected by Physics.SphereCast(), both versions of _Sphere-Raycasting_ uses angle comparison of their own to determine the best object to interact with.

## How to Setup

These explanations will get you through implementing sphere raycast on any Unity Project with versions that allow [Physics.SphereCast()](https://docs.unity3d.com/ScriptReference/Physics.SphereCast.html) or [Physics.RayCast()](https://docs.unity3d.com/ScriptReference/Physics.Raycast.html).

### Requirements

* Unity 2017
* 3D Unity scene
* Any kind of FPS control with camera attached

__In order for GameObjects to be detected by this ray-cast, it must implement _IInteractable_ interface, also provided by this project.__

### Deployment

1. Enabling Ray-cast
* Attach either __DetectInteractableObject.cs__ or __DetectInteractableObjectComparative.cs__ to the FPS character. If main camera is attached to the child, attach it to that child.
* Attach __InteractWithSelectedObject.cs__ to the same GameObject.
* Adjust editor fields to acquire desired range. See comments on scripts for detail.
![gif](https://i.imgur.com/bettbgN.gif)
2. Making Detectable GameObject
* Create a MonoBehaviour class that implements IInteractable
* Add code for desired interaction inside Interact() method, which will be called when __Interact__ input is pressed while this object is detected.
* Attach the created MonoBehaviour script on GameObjects you want player to interact with.

## Example Overview

These explanation describes the provided example scene [Assets/Scenes/SphereCastTest.unity](https://github.com/ALee1303/Sphere-Raycasting/tree/master/Assets/Scenes).

### Requirement and Deployment
* Project Version: Unity2017.3.1
* To avoid version conflict, import package __SphereCastTest.unitypackage__ into an existing project and open the scene.
* I recommend running test with editor screen on to see full functionality.

### Running the Test
* Scene consit of Unity FirstPersonCharacter controller that implements both versions of raycast.
* move around the scene and click left-mouse to see which interactables are dectected on various state.
* Enable one of the Detect scripts attached to FPSController's child to check each functionality.
* White, blue, and pink objects have interactable implemented and will display message on Console Log when interacted.
* red objects are not interactable and will block the interactable objects from being detected.
* Gizmos will be shown on editor screen for details:
  * White lines will be displayed on every object checked by sphere ray-cast.
  * Green line shows which object will be interacted when left-mouse is clicked.
  * Yellow spheres represent __DetectInteractableObject.cs__ range.
  * Red spheres represent __DetectInteractableObjectComparative.cs__ range.

![gif](https://i.imgur.com/ttH5tY8.gif)

## License

This project is licensed under the MIT License - see the [LICENSE](License.md) file for details
