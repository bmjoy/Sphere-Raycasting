# Sphere Ray-cast using Unity

Sphere Ray-casting is a wide-range 3D raycasting method that uses Unity [Physics.SphereCast()](https://docs.unity3d.com/ScriptReference/Physics.SphereCast.html) and [Physics.RayCast()](https://docs.unity3d.com/ScriptReference/Physics.Raycast.html) to detect the best GameObject that can be interacted.

| ![gif](https://i.imgur.com/eSDGxZp.gif) ![gif](https://i.imgur.com/RQWWBCT.gif) |
|:---| 
| ***(Left)** Sphere-RayCasting range is visible by the two red spheres displayed on the editor screen.(The SphereCast sweeps between two red sphere to create a cylindrical range.) The green line indicates that the object has been detected and is not blocked.**(Right)** When more than one object is in range,indicated by the lines drawn towards the object(white lines indicate objects not blocked, green line indicates the prioritized object for interaction), activated object is selected by their angle offset from center of the screen. Notice that when both blue and pink sphere is in-range, green lines interchange depending on the camera's view.* |

## Project Description

This project provides scripts needed to implement Sphere Ray-casting and an example scene that shows how it works.

## Table of Content

<!--ts-->
* [Objective](#objective)
* [Performance Overview](#performance-overview)
  * [Analysis of SphereCast](#analysis-of-spherecast)
  * [Analysis of Block Check](#analysis-of-block-check)
  * [Analysis of Angle Comparison](#analysis-of-angle-comparison)
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
|:---| 
| ***(1)** Succesful block checks are shown by gizmo lines on editor window. White lines are every objects in-range that are not blocked. Green line indicates the best suitable interactable object. **(2)** Blue and Pink Cubes are both interactable objects in range of SphereCast(), displayed by red gizmo sphere in editor window. However, when blocked by the uninteractable red wall, it fails block check. (As shown above when cubes are blocked from player's view, white lines are drawn from player to the red wall, but no lines are drawn towards the interactable cubes)* |

There are two versions of sphere ray-cast:

1. DetectInteractableObject.cs:
* This script simply sorts all objects collected by angle from center then returns the closest one to the center that is not blocked.
2. DetectInteractableObjectComparative.cs:
* This scrit compares the angle between each objects collected and returns the most optimal one. This may not be an object with the smallest angle from the center.
* This script uses greedy algorithm and will have better runtime-complexity than the other one.
* It either returns an object with the closest angle from the camera or closest distance from the player. It also allows a light-weight Observer(Event) patter in designing GameObject interaction.
  * If an object is close enough to center by set angle and is also closest object by distance from player to be so, it becomes an object picked.
  * If no such object was found, the closest object to the center will be returned.

_1 is easier to implement than 2, but 2 has better control and performance._

***Not considering _SphereCast()_ method, 1 Has <img src="https://latex.codecogs.com/gif.latex?O(n^2)" title="O(n^2)" /> worstcase runtime. 2 has <img src="https://latex.codecogs.com/gif.latex?O(n)" title="O(nl)" /> worstcase runtime.***

-------------------------------------------------

### Analysis of SphereCast

Both Sphere-Raycasting method uses Physics.SphereCast() to query every objects collided by the sphere sweeped in front of the player. This returns info of all collided object as minimum heap of RayCastHit sorted by distance from player.

***Note that this collects all objects in range, interactable or not***

```C#
RaycastHit[] allHits; // array-based min-heap containing info of colided objects
allHits = Physics.SphereCastAll(this.transform.position, castRadius,
				this.transform.forward, castDistance); // spherecast to find the objects.
```

 Suppose there were _n_ amounts of objects collided by SphereCast. At worst case, every newly inserted element will be a new minimum in heap, which means it would be the closest object from the play in the heap. If there were _n_ object in the heap prior to the insertion, this would cause the newly inserted minimum to traverse up the height of the binary tree, which would be ![gif](https://latex.codecogs.com/gif.latex?%5Clg%20n). Since every element will be inserted into the heap, performing _n_ number of insert, the worst case runtime of SphereCast is ![gif](https://latex.codecogs.com/gif.latex?O%28n%5Clg%20n%29).

-------------------------------------------------

### Analysis of Block Check

Method for block check is same for both scripts.

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
The function above takes in the object which needs to be block checked as a parameter. It performs a raycast from center of the player's camera towards the object, then check to see if the object first hit by raycast is indeed object passed in as parameter, meaning that there were no other object blocking player's view of the object.

-------------------------------------------------

### Analysis of Angle Comparison

The way two scripts perform angle comparison is crucial in their runtime difference. Not only that, it effects how they pick the suitable interactable object.

**DetectInteractable.cs**:
This script uses C#'s System.Collection's SortedList to sort all objects by their angle offset from center of the screen.
```C#
private SortedList<float,GameObject> interactSortedByAngle;
```
The list takes in every interactable object found by the SphereCast. Before each insertion, the angle from the center is calculated and used as a key to be compared. The script does this by calling the funtion below.
```C#
private void collectInteractables()//find and store all interactables
{
    Vector3 dir = new Vector3();
    float angle;
    for (int i = 0; i < allHits.Length; i++)
    {
        if (allHits[i].collider.GetComponent<IInteractable>() == null)
            continue; // if it's not interactable, skip to next index
        dir = allHits[i].transform.position - this.transform.position; // direction from player towards the object
        angle = Vector3.Angle(this.transform.forward, dir); // angle between center and object
        interactSortedByAngle.Add(angle, allHits[i].collider.gameObject); // add the object to list
    }
}
```
Important information to consider here is how the insertions and deletions are performed. C#'s SortedList uses insertion sort to sort every element every time an insertion is performed. At worst case, each new object inserted will be closer to the center than the prior object, which will cause the object to traverse all-the-way down the list to index 0. If this was the case for all _n_ amount of objects found by SphereCast, the worst case performance time of this function is ![gif](https://latex.codecogs.com/gif.latex?O(n^2)).

Since this script simply calls the closest object from the center of the screen, it can sometimes call objects that may seem unsuitable for interaction.

| ![gif](https://i.imgur.com/jexAVoq.gif) |
|:---|
| *All white objects are interactables. Notice how even though white sphere is barely visible and far away than other objects, the script simply designates the sphere as the suitable interact (designated by green line)* |

**DetectInteractableComparative.cs**:
To solve the problem mentioned above, this script adds few more condition to finding the object by using greedy algorithm and dynamic recursion.
```C#
 private IInteractable GetSuitableInteract(IInteractable prevCandidate = null, int prevAngle = 180, int index = 0)
{
    #region end recursion
    if (index >= allHits.Length)// at the end of the allHits[] index,
        return prevCandidate; // end the recursion. Return whatever was found. null if none was found
    #endregion end recursion
    
    // try to get Interactable from current index if it's not blocked
    IInteractable toCompare
        = GetSuitableInteract(allHits[index].collider.gameObject);
	
    #region ignore angle check
    if (toCompare == null) // ignore anglecheck if toCompare wasn't suitable
        return GetSuitableInteract(prevCandidate, prevAngle, index + 1);
    #endregion ignore angle check
    
    #region setup for angle check
    Vector3 dir = allHits[index].transform.position - this.transform.position; //directional vector towards object to compare
    int angle = (int)Vector3.Angle(this.transform.forward, dir); // angle to be compared
    #endregion setup for angle check

    #region angle checking
    // if angle between center is small enough to just call this candidate
    if (angle <= angleFromCenter)
        return toCompare;
    // if theres nothing to compare or current object is more closer to center by comparativeAngle,
    else if (prevCandidate == null || prevAngle - angle > comparativeAngle)
        return GetSuitableInteract(toCompare, angle, index + 1); // override the previous Interact candidate
    // if all failed, ignore this index and go onto next index
    return GetSuitableInteract(prevCandidate, prevAngle, index + 1);
    #endregion
}
```
*If the object is not interactable, it ignores the whole process and goes to the next index.*

*If the object is interactable, the following process occurs:*
First, if the object being checked is close enough from the center of the screen by given amount _angleFromCenter_, the algorithm will simply ignore proceeding iteration and return that object.
```C#
if (toCompare == null) // ignore anglecheck if toCompare wasn't suitable
    return GetSuitableInteract(prevCandidate, prevAngle, index + 1);
```
If not, it wil compare the calculated angle with that of the previous object. If the comparison is bigger than the given angle _comparativeAngle_, meaning the new object is substantially closer to the center than the candidate before, the new object will replace the previous candidate, becoming the new optimal candidate. This iteration will continue until either the first condition is met, or if it reaches the end of the array.
```C#
else if (prevCandidate == null || prevAngle - angle > comparativeAngle)
    return GetSuitableInteract(toCompare, angle, index + 1); // override the previous Interact candidate
```
The result is subtle yet at some conditions quite noticeable, such as one mentioned above.

| ![gif](https://i.imgur.com/xut6Wj2.gif) |
|:---|
| *Now suitable object is the closer square object.* |

Not only does this algorithm allow better accuracy, but it improves algorithm of angle check to ![gif](https://latex.codecogs.com/gif.latex?O(n)) time.

## How to Setup

These explanations will get you through implementing sphere raycast on any Unity Project with versions that allow [Physics.SphereCast()](https://docs.unity3d.com/ScriptReference/Physics.SphereCast.html) or [Physics.RayCast()](https://docs.unity3d.com/ScriptReference/Physics.Raycast.html).

-------------------------------------------------

### Requirements

* Unity 2017
* 3D Unity scene
* Any kind of FPS control with camera attached

__In order for GameObjects to be detected by this ray-cast, it must implement _IInteractable_ interface, also provided by this project.__

-------------------------------------------------

### Deployment

1. Enabling Ray-cast
* Attach either __DetectInteractableObject.cs__ or __DetectInteractableObjectComparative.cs__ to the FPS character. If main camera is attached to the child, attach it to that child.
* Attach __InteractWithSelectedObject.cs__ to the same GameObject.
* Adjust editor fields to acquire desired range. See comments on scripts for detail.

| ![gif](https://i.imgur.com/bettbgN.gif) |
|:---|
| *example of a correctly implemented inspector using Unity3D's preset FPSController.* |

2. Making Detectable GameObject
* Create a MonoBehaviour class that implements IInteractable
* Add code for desired interaction inside Interact() method, which will be called when __Interact__ input is pressed while this object is detected.
* Attach the created MonoBehaviour script on GameObjects you want player to interact with.

## Example Overview

These explanation describes the provided example scene [Assets/Scenes/SphereCastTest.unity](https://github.com/ALee1303/Sphere-Raycasting/tree/master/Assets/Scenes).

-------------------------------------------------

### Requirement and Deployment
* Project Version: Unity2017.3.1
* To avoid version conflict, import package __SphereCastTest.unitypackage__ into an existing project and open the scene.
* I recommend running test with editor screen on to see full functionality.

-------------------------------------------------

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

| ![gif](https://i.imgur.com/ttH5tY8.gif) |
|:---|
| *Top view of the provided test scene.* |

## License

This project is licensed under the MIT License - see the [LICENSE](License.md) file for details
