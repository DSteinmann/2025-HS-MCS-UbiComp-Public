# Modeling Choices for OptimizedGazeClassifier.ipynb

This document outlines the key methodological choices made in the `OptimizedGazeClassifier.ipynb` notebook for classifying user activity from gaze data.

## 1. Machine Learning Model

- **Model:** XGBoost (Extreme Gradient Boosting)
- **Reason:** XGBoost was chosen for its high performance, speed, and ability to handle a mix of feature types. It is a powerful and widely used algorithm for classification tasks on tabular data.

## 2. Feature Engineering & Preprocessing

- **Feature Selection:** All 19 available gaze features from the dataset were used for training. This approach allows the model to determine feature importance automatically, rather than relying on manual selection. The features used were:
  - `meanFix`: Mean fixation duration.
  - `minFix`: Minimum fixation duration.
  - `maxFix`: Maximum fixation duration.
  - `varFix`: Variance of fixation durations.
  - `stdFix`: Standard deviation of fixation durations.
  - `meanDis`: Mean dispersion (saccade length).
  - `minDis`: Minimum dispersion.
  - `maxDis`: Maximum dispersion.
  - `varDis`: Variance of dispersions.
  - `stdDisp`: Standard deviation of dispersions.
  - `freqDisPerSec`: Frequency of dispersions per second.
  - `number_of_blinks`: Total number of blinks.
  - `blinkMean`: Mean blink duration.
  - `blinkMin`: Minimum blink duration.
  - `blinkMax`: Maximum blink duration.
  - `blinkRate`: Rate of blinks per second.
  - `xDir`: Horizontal direction of gaze movement.
  - `yDir`: Vertical direction of gaze movement.
  - `fixDensPerBB`: Fixation density within the bounding box.
- **Feature Scaling:** `StandardScaler` was applied to all features. This standardizes the data to have a mean of 0 and a standard deviation of 1, which is crucial for the performance of many machine learning algorithms, including XGBoost.
- **Label Encoding:** `LabelEncoder` was used to convert the categorical string labels (`Inspection`, `Reading`, `Search`) into numerical format (integers) that the model can process.

## 3. Hyperparameter Tuning

- **Method:** `GridSearchCV`
- **Description:** A systematic grid search with 5-fold cross-validation was performed to find the optimal hyperparameters for the XGBoost model.
- **Tuned Parameters:**
    - `learning_rate`: [0.05, 0.1, 0.2]
    - `max_depth`: [3, 5, 7]
    - `n_estimators`: [50, 100, 200]

## 4. Evaluation

The model's performance was assessed using several metrics:

- **Accuracy:** The primary metric, which reached **92.2%** on the test set.
- **Classification Report:** Provided detailed precision, recall, and F1-scores for each activity class.
- **Confusion Matrix:** Visualized the model's performance in distinguishing between the different classes.
- **Feature Importance:** A plot was generated to show which gaze features were most influential in the model's predictions.
