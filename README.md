MR Designer is a Mixed Reality applications for creating 3D models using a Valve Index.

# Requirements
- Valve Index, [Steam VR](https://www.steamvr.com "Steam VR")
- [OpenXR SteamVR Passthrough API Layer](https://github.com/Rectus/openxr-steamvr-passthrough/releases)
- [Unity 2021.3.5f1](https://unity.com/releases/editor/whats-new/2021.3.5 "Unity 2021.3.5f1")

# Usage
- [configure the Passthrough API](https://github.com/Rectus/openxr-steamvr-passthrough#usage "configure the Passthrough API")
- open the unity project with the unity editor and click play
- create a 3D Model in VR
	- first add 4 starting nodes
	- then select your desired mode on the menu
		- the menu is attached to your left controller
		- buttons are pressed with the right controller
	- apply the actions to the model using the \"trigger\" button
		- connect requires you to hold the button until 3 or 4 nodes are marked
		- adding or deleting is applied after release
		- a preview node helps selecting triangles for deletion
	- the reset button requires continuously pressing for 1 seconds
- Export the model using the FBX exporter
	- select the \"3D-Model\" GameObject
	- delete all childobjects
	- rightclick the  \"3D-Model\" and select \"Export to FBX...\"
	- click \"Export\"
	
# Demo video

[![MR 3D-Model Designer](http://img.youtube.com/vi/MA0ZtmunfoY/0.jpg)](http://www.youtube.com/watch?v=MA0ZtmunfoY "MR 3D-Model Designer")
