# Report Assignment 2
## Task 1 
For the first task, I've tried multiple approaches to improve performance with the existing model, namely: 
- Different models (XGBoost performed best)
- Feature engineering (adding polynomial features, interaction terms)
- Hyperparameter tuning (using GridSearchCV)
- Cross-validation (to ensure robustness of results)
The final model achieved an accuracy of 92% (on some tasks) on the validation set, which is a significant improvement over the baseline model's 85%.
However, there is still room for improvement, especially in terms of recall and precision for certain classes. This could be addressed by either collecting more data and/or using more advanced models like neural networks.
The final code for Task 1 can be found in `OptimizedGazeClassifier.ipynb`. An explanation of the used classifier choices and hyperparameters is provided in the notebook itself and in the 'OptimizedGazeClassifier_Choices.md' file.

## Task 2
For the second task, I tried to implement multiple solutions into the provided skeleton. The main challenges were: 
- Getting the HoloLens2 solution to work with the provided codebase because of dependency issues.
- Layouting, layouting, layouting... (spent a lot of time on this)
- Dictation integration and testing with holographic remoting
- Pivoting 

The initial idea was to create a mixed-reality application, which feeds a live transcript from the HoloLens2 (for the inspect task) to a note-taking module, which renders the notes near the user. The reading task should've allowed a live feed of the book/article to be send to a VLM, which provides a summary of the read text. The search task should've displayed a grid on the visible space in front of the user, allowing them to search for items in a more structured way. 

However, due to time constraints and technical difficulties with my setup, I was only able to implement very basic version of all the tasks, which are not fully functional. The current implementation includes all the necessary components, but they are not yet integrated properly. Because of Time-reasons, most of the adjustments were made directly in the ActivityReceiver.cs file which is architecturally not the best choice with 1200+ lines of code.

## Task 3
Pretty self explanatory. 

## Task 4
The last task should theoretically work but I didn't get to test it yet. The login and saving my gaze data to a pod was successful, but I didn't get to test the shaaring functionality with another user. 

## Reflection and Suggestions
In general, I'd like to propose a few improvements for the overall assignment structure. I loved the practical approach and the freedom to explore different solutions. Creating apps in XR is always fun and I learned a lot during the process. However, what I found challenging for me (and seemingly for my peers as well), was the used technology. The HoloLens2 is a great starting point but also comes with a lot of limitations and quirks, which made development quite frustrating at times. For me as a web developer, working with Unity and C# was a steep learning curve, which I only managed to somewhat overcome because I got access to it before the assignment. What I would've loved to see is a little more freedom in terms of the used hardware and software stack, for example allowing the usage of VR headsets (if available) or ideally web-based XR solutions like WebXR. This would allow students to leverage their existing skills and focus more on the assignment itself rather than struggling with unfamiliar technology. I'd love to help setting anything up in this direction if needed. 

But overall, I really enjoyed the problem statements and the detailed help from the team. This is still stellar work and I appreciate the effort put into designing this assignment. Looking forward to more!