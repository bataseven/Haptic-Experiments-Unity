clear all
% close all
clc

%Get the experiment file name
[filename, pathname] = uigetfile('*.csv', 'Select a force file');

if isequal(filename, 0) || isequal(pathname, 0)
    disp('File not selected')
    return
else
    disp(['Selected file: ', fullfile(pathname, filename)])
end

%% Import the data and plot
data = readmatrix(fullfile(pathname, filename), 'NumHeaderLines', 0);
experimentDate = split(filename, '_');
subjectName = experimentDate{1};

force = -data(:, 1);
time = data(:, 2);
trial = data(:, 3);

uniqueTrials = unique(trial);

figure
hold on
grid on

% % Plot the force data for each trial in a different color
for j = 1 : length(uniqueTrials)
        plot(time(trial == uniqueTrials(j)), force(trial == uniqueTrials(j)), 'LineWidth', mod(j, 2) + 1)
        % Add the trial number to the plot
        text(time(find(trial == uniqueTrials(j), 1, "first")), force(find(trial == uniqueTrials(j), 1, "first")) + 0.5, num2str(uniqueTrials(j)), 'FontSize', 14)
end

% Add labels and legend
xlabel('Time (s)')
ylabel('Force (N)')
title(['Force vs Time for ', subjectName])
legendInfo = cell(1, length(uniqueTrials));

for j = 1:length(uniqueTrials)
    legendInfo{j} = ['Trial ', num2str(uniqueTrials(j))];
end

legend(legendInfo)

%% Calculate the mean and standard deviation of the force for each trial
meanForce = zeros(1, length(uniqueTrials));
stdForce = zeros(1, length(uniqueTrials));

for j = 1:length(uniqueTrials)
    meanForce(j) = mean(force(trial == uniqueTrials(j)));
    stdForce(j) = std(force(trial == uniqueTrials(j)));
end

%% Plot the mean and standard deviation of the force for each trial
figure
hold on
grid on
errorbar(uniqueTrials, meanForce, stdForce, 'LineWidth', 2)
xlabel('Trial')
ylabel('Force (N)')
title(['Mean and Standard Deviation of Force for ', subjectName])
fprintf('Mean force for %s: %.2fN\n', subjectName, mean(meanForce))
fprintf('Standard deviation of force for %s: %.2fN\n', subjectName, mean(stdForce))