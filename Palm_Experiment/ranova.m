clear
clc

% Data is 540 by 5
load data
% data = reshape(data(:,end), 5, 108);
data = data(1:108,2:end)


% Convert the data to a table format
t = array2table(data, 'VariableNames', {'Velocity', 'Displacement', 'Direction', 'Intensity'});

% Specify the variables for the repeated measures design
rm = table({'Velocity'; 'Displacement'; 'Direction'}, [3; 3; 2], 'VariableNames', {'Factor', 'Levels'});

% Fit a repeated measures model using the variables specified in the rm table
rmModel = fitrm(t, 'Intensity~1', 'WithinDesign', rm, 'WithinModel', 'separatemeans');

% Perform the repeated measures ANOVA
ranovaResults = ranova(rmModel, 'WithinModel', 'separate');

% Print the results
disp(ranovaResults)
