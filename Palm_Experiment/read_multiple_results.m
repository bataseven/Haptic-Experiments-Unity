% close all
clc

answer = questdlg('Do you want to use the file(s) in the workspace?', ...
    'Select New File', ...
    'Yes', 'No', 'No');

%Get the experiment file name
if strcmp(answer, 'No')
    clear all
    [filenames, pathname] = uigetfile('*.csv', 'Select an experiment file', 'MultiSelect', 'on');
else
    % delete every variable except the ones that are needed
    clearvars -except filenames pathname

    if exist('filenames', 'var') ~= 1 || exist('pathname', 'var') ~= 1
        disp('No files in the workspace. Exiting...')
        return
    end

end

% If false, show the figures as subplots
createSeperateFigures = false;

% Which figures to plot
plotCorrectPositions = true;
plotVelocityResults = true;
plotPositionVersusMagnitude = true;

% Maximum number of plots to show side by side if createSeperateFigures is false
maxPlotPerColumn = 4;

if isequal(filenames, 0) || isequal(pathname, 0)
    disp('File not selected')
    return
else

    if iscell(filenames)
        disp(['Selected ', num2str(length(filenames)), ' files'])

        for i = 1:length(filenames)
            disp(['File ', num2str(i), ': ', filenames{i}])
        end

    else
        disp(['Selected file: ', fullfile(pathname, filenames)])
    end

end

if iscell(filenames)
    numFiles = length(filenames);
else
    numFiles = 1;
end

% N by 1 cell array where N is the number of files (Participants)
cumulativeResults = {};

%%
for file = 1:numFiles

    if numFiles == 1
        filename = filenames;
    else
        filename = filenames{file};
    end

    data = readmatrix(fullfile(pathname, filename), 'NumHeaderLines', 0);
    experimentDate = split(filename, '_');
    subjectName = experimentDate{1};
    % isVisualConflicted = contains(filename, ["Conflict", "conflict"]);
    isVisualConflicted = true;

    totalTrials = size(data, 1);
    positions = data(:, 1);
    velocities = data(:, 2);
    directionAnswers = data(:, 3);
    magnitudeAnswers = data(:, 4);
    actualDisplacements = data(:, 5);

    % Find the index of negative velocities and use the index to multiply the positions with -1
    negativeVelocityIndices = find(velocities < 0);

    positions(negativeVelocityIndices) = positions(negativeVelocityIndices) * -1;
    velocities = abs(velocities);

    uniquePositions = unique(positions);
    numberOfPositions = length(uniquePositions);

    uniqueVelocities = unique(velocities);
    numberOfVelocities = length(uniqueVelocities);

    trialPerPosition = totalTrials / numberOfPositions;

    correctIndices = [];
    wrongIndices = [];

    % First column is the value of displacement in mm
    % Second column is the index of correct direction answers
    % Third column is the index of wrong direction answers
    % Fourth column is the correct percentage
    % Fifth column is magnitude answers
    % Sixth column is the average of Fifth
    % Seventh column is the fifth column divided by the sixth column
    results = num2cell(uniquePositions);

    for i = 1:numberOfPositions
        results{i, 2} = [];
        results{i, 3} = [];
        results{i, 4} = 0;
        results{i, 5} = [];
        results{i, 6} = [];
    end

    for i = 1:totalTrials
        directionAnswer = directionAnswers(i);
        magnitudeAnswer = magnitudeAnswers(i);
        position = positions(i);
        positionIndex = find([results{:, 1}] == position);

        isCorrect = position * directionAnswer > 0;

        if isCorrect
            results{positionIndex, 2} = [results{positionIndex, 2} i]; % Correct answer
        else
            results{positionIndex, 3} = [results{positionIndex, 3} i]; % Wrong answer
        end

        results{positionIndex, 4} = length(results{positionIndex, 2}) / trialPerPosition * 100;
        results{positionIndex, 5} = [results{positionIndex, 5} magnitudeAnswer];
        results{positionIndex, 6} = geomean(results{positionIndex, 5});
    end

    results{positionIndex, 7} = results{positionIndex, 5} / results{positionIndex, 6};

    if (plotCorrectPositions)

        if ~createSeperateFigures
            figure(1)
            subplot(ceil(numFiles / maxPlotPerColumn), min(numFiles, maxPlotPerColumn), file)
        else
            figure
        end

        bar([results{:, 1}], [results{:, 4}])
        ylim([0 100])
        xlim([min([results{:, 1}]) - 5, max([results{:, 1}]) + 5])
        ylabel('Percent Correct (%)')
        xlabel('Displacement in mm')
        title([{['Subject: ', subjectName], experimentDate{2}}])
    end

    fprintf("Direction identification accuracy (%s): %.2f\n", subjectName, mean([results{:, 4}]))

    % N by 1 cell array where N is the number of unique velocities
    % Each cell contains a cell array with the same structure as results
    cumulativeVelocityResults = {};

    % I hate myself for implementing this like this

    % First column is the value of displacement in mm
    % Second column is the index of correct direction answers
    % Third column is the index of wrong direction answers
    % Fourth column is the correct percentage
    % Fifth column is magnitude answers
    % Sixth column is the average of Fifth
    % Seventh column is the fifth column divided by the sixth column

    % Create a cell array for each velocity
    for i = 1:numberOfVelocities
        velocity = uniqueVelocities(i);
        eval(['resultsVelocity' num2str(velocity * 100) ' = num2cell(uniquePositions);']);

        for i = 1:numberOfPositions
            eval(['resultsVelocity' num2str(velocity * 100) '{i, 2} = [];']);
            eval(['resultsVelocity' num2str(velocity * 100) '{i, 3} = [];']);
            eval(['resultsVelocity' num2str(velocity * 100) '{i, 4} = 0;']);
            eval(['resultsVelocity' num2str(velocity * 100) '{i, 5} = [];']);
            eval(['resultsVelocity' num2str(velocity * 100) '{i, 6} = [];']);
        end

    end

    for i = 1:totalTrials
        i;
        directionAnswer = directionAnswers(i);
        magnitudeAnswer = magnitudeAnswers(i);
        position = positions(i);
        velocity = velocities(i);
        positionIndex = eval(['find([resultsVelocity' num2str(velocity * 100) '{:,1}] == position);']);

        isCorrect = directionAnswer * position > 0;

        if isCorrect
            eval(['resultsVelocity' num2str(velocity * 100) '{positionIndex, 2} = [resultsVelocity' num2str(velocity * 100) '{positionIndex, 2} i];']);
        else
            eval(['resultsVelocity' num2str(velocity * 100) '{positionIndex, 3} = [resultsVelocity' num2str(velocity * 100) '{positionIndex, 3} i];']);
        end

        trialCount = length(eval(['resultsVelocity' num2str(velocity * 100) '{positionIndex, 2}'])) + length(eval(['resultsVelocity' num2str(velocity * 100) '{positionIndex, 3}']));
        eval(['resultsVelocity' num2str(velocity * 100) '{positionIndex, 4} = length(resultsVelocity' num2str(velocity * 100) '{positionIndex, 2}) / trialCount * 100;']);
        eval(['resultsVelocity' num2str(velocity * 100) '{positionIndex, 5} = [resultsVelocity' num2str(velocity * 100) '{positionIndex, 5} magnitudeAnswer];']);
        eval(['resultsVelocity' num2str(velocity * 100) '{positionIndex, 6} = geomean(resultsVelocity' num2str(velocity * 100) '{positionIndex, 5});']);
        eval(['resultsVelocity' num2str(velocity * 100) '{positionIndex, 7} = resultsVelocity' num2str(velocity * 100) '{positionIndex, 5} / resultsVelocity' num2str(velocity * 100) '{positionIndex, 6};']);
    end

    for i = 1:numberOfVelocities
        velocity = uniqueVelocities(i);
        cumulativeVelocityResults{i, 1} = eval(['resultsVelocity' num2str(velocity * 100)]);
    end

    cumulativeResults{file, 1} = cumulativeVelocityResults;

    % Plot the correct percentage of direction answers on the same bar graph
    if plotVelocityResults

        if (~createSeperateFigures)
            figure(2)
            subplot(ceil(numFiles / maxPlotPerColumn), min(numFiles, maxPlotPerColumn), file)
        else
            figure
        end

        x = uniqueVelocities;
        y = [];

        for i = 1:numberOfVelocities
            velocity = uniqueVelocities(i);
            velocityResults = [];

            for j = 1:numberOfPositions
                velocityResults = [velocityResults eval(['resultsVelocity' num2str(velocity * 100) '{j, 6}'])];
            end

            y = [y; velocityResults];
        end

        bar(x, y)
        title([{['Subject: ', subjectName], experimentDate{2}}])

        if createSeperateFigures
            legendPos = 'northeast';
        else
            legendPos = 'southwest';
        end

        legends = [];

        for i = 1:numberOfPositions
            legends = [legends, [strcat("\Delta x = ", num2str(uniquePositions(i)), "mm")]];
        end

        legend(legends, 'Location', legendPos)
        xlabel('Velocity (mm/s)')
        ylabel('Magnitude')
        % ylim([0 100])
        xticks(uniqueVelocities)
    end

    if plotPositionVersusMagnitude

        if (~createSeperateFigures)
            figure(3)
            subplot(ceil(numFiles / maxPlotPerColumn), min(numFiles, maxPlotPerColumn), file)
        else
            figure
        end

        x = uniqueVelocities;
        y = [];

        % N by M cell array where N is the number of velocities and M is the number of positions
        % Each cell contains a vector of magnitudes for that velocity and position
        velocitiesMagnitudes = cell(numberOfVelocities, numberOfPositions);
        % Fill the cell array with empty vectors
        for i = 1:numberOfVelocities

            for j = 1:numberOfPositions
                velocitiesMagnitudes{i, j} = [];
            end

        end

        % Fill the cell array with the magnitudes for each velocity and position combination from each file
        for i = 1:numberOfVelocities

            for j = 1:numberOfPositions

                magnitudes = cumulativeResults{file, 1}{i, 1}{j, 5};
                velocitiesMagnitudes{i, j} = [velocitiesMagnitudes{i, j} magnitudes];

            end

        end

        grandGeoMeans = zeros(numberOfVelocities, numberOfPositions);
        % Calculate the geometric means for each velocity and position combination
        for i = 1:numberOfVelocities

            for j = 1:numberOfPositions
                grandGeoMeans(i, j) = geomean(velocitiesMagnitudes{i, j});
            end

        end

        dataToPlot = cell(numberOfVelocities, numberOfPositions);
        % Fill the cell array with empty vectors
        for i = 1:numberOfVelocities

            for j = 1:numberOfPositions
                dataToPlot{i, j} = [];
            end

        end

        standartDevs = [];
        % Create the data to plot by taking the arithmetic mean of the normalized magnitudes and
        % multiplying it by the grand geometric mean for that velocity and position combination
        for i = 1:numberOfVelocities
            standartDev = [];

            for j = 1:numberOfPositions

                resultsForACondition = [];

                value = mean(cumulativeResults{file, 1}{i, 1}{j, 7}) * grandGeoMeans(i, j);
                standartDev = [standartDev std(cumulativeResults{file, 1}{i, 1}{j, 7}) * grandGeoMeans(i, j)];
                resultsForACondition = [resultsForACondition value];

                dataToPlot{i, j} = resultsForACondition;

            end

            standartDevs = [standartDevs; standartDev];

        end

        markers = ['o', 's', 'd', 'x', 'v', '^', '>', '<', 'p', 'h'];
        lines = ['-', ':', '--', '-.', '-', ':', '--', '-.', '-', ':'];
        x = uniquePositions;
        % Plot the data points with a different marker for each velocity
        for i = 1:numberOfVelocities
            y = [];

            for j = 1:numberOfPositions
                y = [y mean(dataToPlot{i, j})];
            end

            errorbar(x, y, standartDevs(i, :), 'Marker', markers(i), 'LineStyle', lines(i), 'LineWidth', 1.5, 'MarkerSize', 8)
            hold on
        end

        title([{['Subject: ', subjectName], experimentDate{2}}])

        if createSeperateFigures
            legendPos = 'northeast';
        else
            legendPos = 'southwest';
        end

        legends = [];

        for i = 1:numberOfVelocities
            legends = [legends, [strcat("Velocity ", num2str(uniqueVelocities(i)), "mm/s")]];
        end

        legend(legends, 'Location', legendPos)
        xlabel('Tactor Displacement [mm]')
        ylabel('Magnitude')
        xticks(uniquePositions)
    end

end
