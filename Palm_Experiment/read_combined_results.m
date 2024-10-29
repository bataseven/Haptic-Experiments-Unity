close all
clc

answer = questdlg('Do you want to use the file(s) in the workspace?', ...
    'Select New File', ...
    'Yes', 'No', 'Yes');

%Get the experiment file name
if strcmp(answer, 'No')
    clear all
    [filenames, pathname] = uigetfile('*.csv', 'Select an experiment file', 'MultiSelect', 'on');
else
    clearvars -except filenames pathname

    if exist('filenames', 'var') ~= 1 || exist('pathname', 'var') ~= 1
        disp('No files in the workspace. Exiting...')
        return
    end

end

% If false, show the figures as subplots
createSeperateFigures = false;

plotCumulativeResults = true;

% Which figures to plot
plotCorrectPositions = false;
plotVelocityResults = false;
plotPositionVersusMagnitude = false;

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

fprintf("\n");

if iscell(filenames)
    numFiles = length(filenames);
else
    numFiles = 1;
end

% N by 1 cell array where N is the number of files (Participants)
cumulativeResults = {};

subjectNames = [];

% M by K cell array where M is the number of velocities and K is the number of positions
cumulativeMeans = [];
%%
for file = 1:numFiles

    if numFiles == 1
        filename = filenames;
    else
        filename = filenames{file};
    end

    data = readmatrix(fullfile(pathname, filename), 'NumHeaderLines', 0);
    experimentDate = split(filename, '_');
    subjectName = convertCharsToStrings(experimentDate{1});
    subjectNames = [subjectNames subjectName];
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
    trialPerPosition = totalTrials / numberOfPositions;

    % Count the number of unique velocities
    uniqueVelocities = unique(velocities);
    numberOfVelocities = length(uniqueVelocities);

    % Initialize the cumulative means
    if file == 1
        cumulativeMeans = zeros(numberOfVelocities, numberOfPositions);
    end

    correctIndices = [];
    wrongIndices = [];

    % First column is the value of displacement in mm
    % Second column is the index of correct direction answers
    % Third column is the index of wrong direction answers
    % Fourth column is the correct percentage
    % Fifth column is magnitude answers
    % Sixth column is the mean of Fifth
    % Seventh column is the fifth column divided by the sixth column
    results = num2cell(uniquePositions);

    for i = 1:numberOfPositions
        results{i, 2} = [];
        results{i, 3} = [];
        results{i, 4} = 0;
        results{i, 5} = [];
        results{i, 6} = [];
        results{i, 7} = [];
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
            eval(['resultsVelocity' num2str(velocity * 100) '{i, 7} = [];']);
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

    means = [];

    for i = 1:numberOfVelocities
        velocity = uniqueVelocities(i);
        cumulativeVelocityResults{i, 1} = eval(['resultsVelocity' num2str(velocity * 100)]);
        means = [means; eval(['[resultsVelocity' num2str(velocity * 100) '{:,6}];'])];
    end

    cumulativeMeans = cumulativeMeans + means;

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

        x = uniquePositions;
        y = [];

        for i = 1:numberOfVelocities
            velocity = uniqueVelocities(i);
            velocityResults = [];

            for j = 1:numberOfPositions
                velocityResults = [velocityResults eval(['resultsVelocity' num2str(velocity * 100) '{j, 6}'])];
            end

            y = [y; velocityResults];
        end

        markers = ['o', 's', 'd', 'x', 'v', '^', '>', '<', 'p', 'h'];
        lines = ['-', ':', '--', '-.', '-', ':', '--', '-.', '-', ':'];

        hold on

        % Plot the data points with a different marker for each velocity
        for i = 1:numberOfVelocities
            plot(x, y(i, :), 'Marker', markers(i), 'LineStyle', lines(i), 'LineWidth', 1.5, 'MarkerSize', 8)
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

cumulativeMeans = cumulativeMeans / numFiles;

% Plot the cumulative results
if plotCumulativeResults

    figure

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

            for file = 1:numFiles
                magnitudes = cumulativeResults{file, 1}{i, 1}{j, 5};
                velocitiesMagnitudes{i, j} = [velocitiesMagnitudes{i, j} magnitudes];
            end

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
    identificationAccuracies = cell(numberOfVelocities, numberOfPositions);
    % Fill the cell array with empty vectors
    for i = 1:numberOfVelocities

        for j = 1:numberOfPositions
            dataToPlot{i, j} = [];
        end

    end

    % Create the data to plot by taking the arithmetic mean of the normalized magnitudes and
    % multiplying it by the grand geometric mean for that velocity and position combination
    for i = 1:numberOfVelocities

        for j = 1:numberOfPositions

            resultsForACondition = [];
            accuracyForACondition = [];

            for file = 1:numFiles
                value = mean(cumulativeResults{file, 1}{i, 1}{j, 7}) * grandGeoMeans(i, j);
                accuracy = cumulativeResults{file, 1}{i, 1}{j, 4};

                resultsForACondition = [resultsForACondition value];
                accuracyForACondition = [accuracyForACondition accuracy];
            end

            dataToPlot{i, j} = resultsForACondition;
            identificationAccuracies{i, j} = accuracyForACondition;
        end

    end

    identificationAccuraciesMeans = zeros(numberOfVelocities, numberOfPositions);
    identificationAccuraciesStd = zeros(numberOfVelocities, numberOfPositions);

    for i = 1:numberOfVelocities

        for j = 1:numberOfPositions
            identificationAccuraciesMeans(i, j) = mean(identificationAccuracies{i, j});
            identificationAccuraciesStd(i, j) = std(identificationAccuracies{i, j});
        end

    end

    % If it is the last file print the mean and the std of the identification accuracies
    if file == numFiles && numFiles > 1
        fprintf("\nOverall identification accuracy for %g participants: %g +- %g\n", numFiles, mean(identificationAccuraciesMeans(:)), std(identificationAccuraciesMeans(:)))
    end

    markers = ['o', 's', 'd', 'x', 'v', '^', '>', '<', 'p', 'h'];
    lines = ['-', ':', '--', '-.', '-', ':', '--', '-.', '-', ':'];
    x = uniquePositions;
    % Plot the data points with a different marker for each velocity
    velocityCurvePoints = [];

    for i = 1:numberOfVelocities
        y = [];
        standartDev = [];

        for j = 1:numberOfPositions
            y = [y mean(dataToPlot{i, j})];
            standartDev = [standartDev std(dataToPlot{i, j})];
        end

        velocityCurvePoints = [velocityCurvePoints; y];
        errorbar(x, y, standartDev, 'Marker', markers(i), 'LineStyle', lines(i), 'LineWidth', 1.5, 'MarkerSize', 8)
        hold on
    end

    % If there are more than 1 files, set the title
    if numFiles == 1
        title([{strcat("Subject: ", subjectName), experimentDate{2}}])
    else
        title([{[num2str(numFiles), ' Subjects'], "Cumulative Results"}])
    end

    if createSeperateFigures
        legendPos = 'northeast';
    else
        legendPos = 'southwest';
    end

    legends = [];

    for i = 1:numberOfVelocities
        legends = [legends, [strcat("Velocity ", num2str(uniqueVelocities(i)), " mm/s")]];
    end

    legend(legends, 'Location', legendPos)
    xlabel('Tactor Displacement [mm]')
    ylabel('Magnitude (Normalized)')
    xticks(uniquePositions)

    % Increase the font size
    ax = gca;
    ax.FontSize = 16;
    ax.FontWeight = 'bold';
    ax.FontName = 'Times New Roman';

    % Plot surface fits
    radialPositions = abs(uniquePositions(numel(uniquePositions) / 2 + 1:end));
    ulnarPositions = abs(uniquePositions(1:numel(uniquePositions) / 2));

    % Create an array of position and velocity combinations
    % Each row is a combination, the first column is the position and the second column is the velocity
    radialIndependentVars = [];
    [pos, vel] = meshgrid(uniqueVelocities, radialPositions);
    radialIndependentVars = [pos(:) vel(:)];

    ulnarIndependentVars = [];
    [pos, vel] = meshgrid(uniqueVelocities, ulnarPositions);
    ulnarIndependentVars = [pos(:) vel(:)];

    % Create dependent variables for the radial and ulnar positions. These are the mean values for each velocity and position combination
    radialDependentVars = [];
    ulnarDependentVars = [];

    for i = 1:numberOfVelocities

        for j = 1:numel(ulnarPositions)
            ulnarDependentVars = [ulnarDependentVars; mean(dataToPlot{i, j})];
        end

        for j = numel(ulnarPositions) + 1:numberOfPositions
            radialDependentVars = [radialDependentVars; mean(dataToPlot{i, j})];
        end

    end

    % p_radial = polyfitn(radialIndependentVars, radialDependentVars, 'constant X1 X2 X1*X2');
    p_radial = polyfitn(radialIndependentVars, radialDependentVars, 2);
    p_r = polyn2sympoly(p_radial); % Print the radial surface fit equation
    % p_ulnar = polyfitn(ulnarIndependentVars, ulnarDependentVars, 'constant X1 X2 X1*X2');
    p_ulnar = polyfitn(ulnarIndependentVars, ulnarDependentVars, 2);
    p_u = polyn2sympoly(p_ulnar); % Print the ulnar surface fit equation

    % Print the surface fits
    fprintf("\nRadial Surface Fit: ")
    disp(p_r)
    fprintf("Ulnar Surface Fit: ")
    disp(p_u)

    numberOfPointsOnGrid = 20;
    vel_grid = linspace(min(uniqueVelocities), max(uniqueVelocities), numberOfPointsOnGrid);
    % vel_grid = linspace(-1000, 1000, numberOfPointsOnGrid);

    figure % Create a new figure for the surface fits
    subplot(1, 2, 1) % Create a subplot for the radial surface fit
    % Make a finer grid by increasing the number of points
    pos_grid = linspace(min(radialPositions), max(radialPositions), numberOfPointsOnGrid);
    % pos_grid = linspace(-1000, 1000, numberOfPointsOnGrid);
    [X_v, Y_p] = meshgrid(vel_grid, pos_grid);
    Z2 = polyvaln(p_radial, [X_v(:) Y_p(:)]);
    Z2 = reshape(Z2, size(X_v));
    surf(X_v, Y_p, Z2, 'FaceAlpha', 0.35)
    hold on
    scatter3(radialIndependentVars(:, 1), radialIndependentVars(:, 2), radialDependentVars, 'filled')
    xlabel('Velocity [mm/s]')
    ylabel('Tactor Displacement [mm]')
    zlabel('Magnitude (Normalized)')
    title('Radial Surface Fit')

    subplot(1, 2, 2) % Create a subplot for the ulnar surface fit
    % Make a finer grid by increasing the number of points
    pos_grid = linspace(min(ulnarPositions), max(ulnarPositions), numberOfPointsOnGrid);
    [X_v, Y_p] = meshgrid(vel_grid, pos_grid);
    Z2 = polyvaln(p_ulnar, [X_v(:) Y_p(:)]);
    Z2 = reshape(Z2, size(X_v));
    surf(X_v, Y_p, Z2, 'FaceAlpha', 0.35)
    hold on
    scatter3(ulnarIndependentVars(:, 1), ulnarIndependentVars(:, 2), ulnarDependentVars, 'filled')
    xlabel('Velocity [mm/s]')
    ylabel('Tactor Displacement [mm]')
    zlabel('Magnitude (Normalized)')
    title('Ulnar Surface Fit')

    % Do the surface fit without seperating the radial and ulnar positions
    % Create an array of position and velocity combinations
    % Each row is a combination, the first column is the position and the second column is the velocity
    independentVars = [];
    [pos, vel] = meshgrid(uniqueVelocities, uniquePositions);
    independentVars = [pos(:) vel(:)];

    dependentVars = [];

    for i = 1:numberOfVelocities

        for j = 1:numberOfPositions
            dependentVars = [dependentVars; mean(dataToPlot{i, j})];
        end

    end

    p_single = polyfitn(independentVars, dependentVars, 2);
    p_single_text = polyn2sympoly(p_single); % Print the surface fit equation
    fprintf("Surface Fit: ")
    disp(p_single_text)

    figure

    % Make a finer grid by increasing the number of points
    pos_grid = linspace(min(uniquePositions), max(uniquePositions), numberOfPointsOnGrid);
    [X_v, Y_p] = meshgrid(vel_grid, pos_grid);
    Z2 = polyvaln(p_single, [X_v(:) Y_p(:)]);
    Z2 = reshape(Z2, size(X_v));
    surf(X_v, Y_p, Z2, 'FaceAlpha', 0.35)
    hold on
    scatter3(independentVars(:, 1), independentVars(:, 2), dependentVars, 'filled')
    xlabel('Velocity [mm/s]')
    ylabel('Tactor Displacement [mm]')
    zlabel('Magnitude (Normalized)')
    % title('Surface Fit')

    % Increase the font size
    ax = gca;
    ax.FontSize = 16;
    ax.FontWeight = 'bold';
    ax.FontName = 'Times New Roman';

    % Built-in MATLAB function to do the surface fit
    figure
    subplot(1, 3, 1)
    builtin_surface_fit = fit(independentVars, dependentVars, 'poly22');
    plot(builtin_surface_fit, independentVars, dependentVars)

    subplot(1, 3, 2)
    builtin_surface_fit = fit(ulnarIndependentVars, ulnarDependentVars, 'poly22');
    plot(builtin_surface_fit, ulnarIndependentVars, ulnarDependentVars)

    subplot(1, 3, 3)
    builtin_surface_fit = fit(radialIndependentVars, radialDependentVars, 'poly22');
    plot(builtin_surface_fit, radialIndependentVars, radialDependentVars)

    %  Use dataToPlot to generate values for 3 way ANOVA and repeated measures ANOVA
    %  For the 3 way ANOVA, first group is velocity, second group is absolute displacement, third group is the displacement direction

    groups = [];
    responses = [];

    for i = 1:numberOfVelocities

        for j = 1:numberOfPositions

            responses = [responses; dataToPlot{i, j}'];

            for k = 1:length(dataToPlot{i, j})
                stretchDirection = uniquePositions(j) / abs(uniquePositions(j));
                tactorDisplacement = abs(uniquePositions(j));
                velocity = uniqueVelocities(i);
                groups = [groups; velocity, tactorDisplacement, stretchDirection];
            end

        end

    end

    %  Perform the 3 way ANOVA
    % [p_values, tbl, stats] = anovan(responses, groups, 'varnames', {'Velocity', 'Displacement', 'Stretch Direction'}, 'display', 'on');

    % Create the data N by M where N is the number of files and M is the number of conditions
    newData = zeros(numFiles, numberOfVelocities * numberOfPositions);
    newDataToPlot = cell(size(dataToPlot));

    for i = 1:numberOfVelocities
        newDataToPlot{i, 1} = dataToPlot{i, 1};
        newDataToPlot{i, 2} = dataToPlot{i, 6};
        newDataToPlot{i, 3} = dataToPlot{i, 2};
        newDataToPlot{i, 4} = dataToPlot{i, 5};
        newDataToPlot{i, 5} = dataToPlot{i, 3};
        newDataToPlot{i, 6} = dataToPlot{i, 4};
    end

    for i = 1:numFiles

        for j = 1:numberOfVelocities

            for k = 1:numberOfPositions

                newData(i, (j - 1) * numberOfPositions + k) = newDataToPlot{j, k}(i);

            end

        end

    end

    % Create data to use with RMAOV33.
    % X - data matrix
    % Size of matrix must be n-by-5;
    % dependent variable                       = column 1
    % independent variable 1 (within subjects) = column 2
    % independent variable 2 (within subjects) = column 3
    % independent variable 3 (within subjects) = column 4
    % subject                                  = column 5.

    %                                  T1                                           T2
    %             -----------------------------------------------------------------------------------------
    %                   C1             C2             C3             C1             C2             C3
    %             -----------------------------------------------------------------------------------------
    %    Subject   M1   M2   M3   M1   M2   M3   M1   M2   M3   M1   M2   M3   M1   M2   M3   M1   M2   M3
    %    --------------------------------------------------------------------------------------------------
    %       1      10    8    6    9    7    5    7    6    3    5    4    3    4    3    3    2    2    1
    %       2       9    8    5   10    6    4    4    5    2    4    3    3    4    2    2    2    3    2
    %       3       8    7    4    7    4    3    3    4    2    4    1    2    3    3    2    1    0    1
    %    --------------------------------------------------------------------------------------------------

    X_v = zeros(numFiles * numberOfVelocities * numberOfPositions, 5);

    for i = 1:numFiles

        for j = 1:numberOfVelocities

            for k = 1:numberOfPositions

                X_v((i - 1) * numberOfVelocities * numberOfPositions + (j - 1) * numberOfPositions + k, 1) = newData(i, (j - 1) * numberOfPositions + k);
                X_v((i - 1) * numberOfVelocities * numberOfPositions + (j - 1) * numberOfPositions + k, 2) = j;
                X_v((i - 1) * numberOfVelocities * numberOfPositions + (j - 1) * numberOfPositions + k, 3) = k;
                X_v((i - 1) * numberOfVelocities * numberOfPositions + (j - 1) * numberOfPositions + k, 4) = uniquePositions(k) / abs(uniquePositions(k));
                X_v((i - 1) * numberOfVelocities * numberOfPositions + (j - 1) * numberOfPositions + k, 5) = i;

            end

        end

    end

    % Perform the repeated measures ANOVA
    %     RMAOV33(X_v, 0.05)

end
