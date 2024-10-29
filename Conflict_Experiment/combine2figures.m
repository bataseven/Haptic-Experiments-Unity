% This script prompts a window to select .fig files and shows them in a single figure as subplots
% Get the .fig files
[FileName, PathName] = uigetfile('*.fig', 'Select the .fig files', 'MultiSelect', 'on');

% Open each .fig file and store the handles in a cell array
if iscell(FileName)

    for i = 1:length(FileName)
        h{i} = openfig([PathName FileName{i}]);
        h{i}.Position = [100 100 600 600];
    end

else
    h{1} = openfig([PathName FileName]);
end

%% Plot

new_fig = figure;
ax_new = gobjects(size(h));

for i = 1:numel(h)
    ax = subplot(1, numel(h), i);
    ax_old = findobj(h{i}, 'type', 'axes');
    ax_new(i) = copyobj(ax_old, new_fig);
    ax_new(i).YLimMode = 'manual';
    ax_new(i).Position = ax.Position;
    ax_new(i).Position(4) = ax_new(i).Position(4) - 0.02;
    delete(ax);
end
