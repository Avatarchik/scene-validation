# Scene Validation

## Description
A tool which attempts to validate the state of a scene before entering play mode. 
This is used, for example, to avoid waiting a long time to enter play mode, only to realize that a prefab reference or similar has not been set up correctly.
In such a case, the validator would instead log errors and abort entering play mode.
