import numpy as np
import matplotlib.pyplot as plt
import matplotx
from time import time

# plt.style.use(matplotx.styles.github["dimmed"])

FOLDER_NAME = "ForceData"
FILE_INDEX = 8

SLIDE_LIMIT = 100  # The maximum number of points to show on the plot at once

play = False


def key_press(event):
    global play
    if event.key == "escape":
        plt.close()
        exit()
    elif not play:
        play = not play


def main():
    print(f"Importing {FOLDER_NAME}/{FILE_INDEX}.csv")
    # Read the data from the file
    try:
        data = np.genfromtxt(f"{FOLDER_NAME}/{FILE_INDEX}.csv", delimiter=",")
    except FileNotFoundError:
        print(f"File {FOLDER_NAME}/{FILE_INDEX}.csv not found")
        exit()
    force_time = data[:, 0]
    user_force = data[:, 1]
    desired_force = data[:, 2]
    print("Data imported")

    font_size = 20

    # Create a figure and 2 subplots
    fig, ax = plt.subplots(ncols=1, nrows=2, gridspec_kw={
                           "height_ratios": [6, 1]})
    fig.canvas.mpl_connect("key_press_event", key_press)

    # Set figure height to screen height
    fig.set_size_inches(10, 7.5)

    desired_force_line, = ax[0].plot(
        [], [], "m-", label="Desired Force", linewidth=10, alpha=0.8, animated=True)
    user_force_line, = ax[0].plot(
        [], [], "y-", label="User Force", linewidth=10, alpha=0.6, animated=True)
    
    # Create a legend with invisible box
    ax[0].legend(loc="upper left", fontsize=font_size, framealpha=0)

    # Set the title
    ax[0].set_title(f"Forces vs Time", fontsize=font_size)

    ax[0].set_xlim(0, force_time[-1])
    ax[0].set_ylim(-2.5, 2.5)

    ax[0].get_xaxis().set_animated(True)
    ax[0].get_yaxis().set_animated(True)

    # plt.xticks(fontsize=100)
    # plt.yticks(fontsize=100)
    

    # Turn off the ticks on both axes
    ax[1].tick_params(axis="both", which="both", bottom=False, top=False,
                      labelbottom=False, right=False, left=False, labelleft=False)

    ax[1].spines['top'].set_visible(False)
    ax[1].spines['right'].set_visible(False)
    ax[1].spines['bottom'].set_visible(False)
    ax[1].spines['left'].set_visible(False)
    
    outerBox = plt.Rectangle((0.1, 0.1), 0.8, 0.8,
                             fc="w", fill=False, edgecolor='k', linewidth=2)
    innerBox = plt.Rectangle((0, 0.1), 0, 0.8,
                             fc="r", fill=True, edgecolor='none', linewidth=0)

    ax[1].add_patch(outerBox)
    ax[1].add_patch(innerBox)
    
    desired_force_line.set_linewidth(2)
    user_force_line.set_linewidth(2)
    plt.tight_layout()
    plt.subplots_adjust(hspace=0.2)
    plt.show(block=False)

    fig.canvas.resize_event()

    plt.pause(1)
    bg = fig.canvas.copy_from_bbox(fig.bbox)
    fig.canvas.blit(fig.bbox)
    ax[0].draw_artist(ax[0].get_xaxis())
    ax[0].draw_artist(ax[0].get_yaxis())
    ax[0].draw_artist(desired_force_line)
    ax[0].draw_artist(user_force_line)
    ax[1].draw_artist(outerBox)
    ax[1].draw_artist(innerBox)
    max_force = max(abs(user_force))

    print("Press escape to exit, any other key to pause/play")
    N = len(force_time)
    for i in range(N):
        while not play:
            if not plt.fignum_exists(fig.number):
                break
            plt.pause(0.2)

        if not plt.fignum_exists(fig.number):
            break

        if i == 0:
            pause_time = 0.001
        else:
            pause_time = force_time[i] - force_time[i - 1]

        startIndex = max(0, i - SLIDE_LIMIT)
        endIndex = i + 1

        user_force_window = user_force[startIndex:endIndex]
        desired_force_window = desired_force[startIndex:endIndex]
        time_window = force_time[startIndex:endIndex]

        # fig.canvas.resize_event()
        fig.canvas.restore_region(bg)

        user_force_line.set_xdata(time_window)
        user_force_line.set_ydata(user_force_window)

        desired_force_line.set_xdata(time_window)
        desired_force_line.set_ydata(desired_force_window)

        max_force_window = max(max(user_force_window),
                               max(desired_force_window))
        min_force_window = min(min(user_force_window),
                               min(desired_force_window))

        

        ylim = ax[0].set_ylim(min(-2.5, min_force_window - abs(min_force_window) * 0.1),
                       max(2.5, max_force_window + abs(max_force_window) * 0.1))

        if i > SLIDE_LIMIT:
            ax[0].set_xlim(force_time[startIndex], force_time[i])
        else:
            ax[0].set_xlim(
                0, force_time[min(SLIDE_LIMIT, len(force_time) - 1)])


        print(f"{i}/{N} ({100 * i/N:.1f}%)", f"\tTime: {force_time[i]:.2f}", f"\tForce: {user_force[i]:.2f}",
              f"\tDesired Force: {desired_force[i]:.2f}", f"\tPause Time: {pause_time:.2f}")

        # Set the inner box size to the current force
        width = abs(user_force[i] / max_force * 0.8)

        # Paint the box red if the force is negative
        # Make the color brighter if the force is larger
        if user_force[i] < 0:
            innerBox.set_facecolor((1, 0, 0, width / 0.8 / 2 + 0.5))
        else:
            innerBox.set_facecolor((0, 1, 0, width / 0.8 / 2 + 0.5))
        pos_x = 0.1 + (0.8 - width) / 2
        innerBox.set_width(width)
        innerBox.set_x(pos_x)
        ax[0].draw_artist(ax[0].get_yaxis())
        ax[0].draw_artist(ax[0].get_yaxis())        
        ax[0].draw_artist(desired_force_line)
        ax[0].draw_artist(user_force_line)
        ax[1].draw_artist(outerBox)
        ax[1].draw_artist(innerBox)
        fig.canvas.blit(fig.bbox)
        # fig.canvas.flush_events()
    else:
        input("Press enter to close the plot")


if __name__ == '__main__':
    main()
