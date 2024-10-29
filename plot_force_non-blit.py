import threading
from PyQt5 import QtCore
import numpy as np
import matplotlib.pyplot as plt
import matplotlib
from time import time
import signal
import sys

matplotlib.use("Qt5Agg")
# matplotlib.use("TkAgg")  
# plt.style.use(matplotx.styles.github["dimmed"])
    # Set font name
matplotlib.rcParams["font.family"] = "Times New Roman"

FOLDER_NAME = "ForceData"
FILE_INDEX = 9
SLIDE_LIMIT = 100  # The maximum number of points to show on the plot at once

play = False
keep_window_open = False


def signal_handler(sig, frame):
    print('Exiting...')
    sys.exit(0)

signal.signal(signal.SIGINT, signal_handler)

def key_press(event):
    global keep_window_open
    if event.key == "escape":
        plt.close()
        exit()
    elif not keep_window_open:
        global play
        play = not play
    elif keep_window_open and event.key == "q":
        keep_window_open = False


def main():
    print(f"Importing {FOLDER_NAME}/{FILE_INDEX}.csv")
    # Read the data from the file
    try:
        data = np.genfromtxt(f"{FOLDER_NAME}/{FILE_INDEX}.csv", delimiter=",")
    except FileNotFoundError:
        print(f"File {FOLDER_NAME}/{FILE_INDEX}.csv not found")
        exit()
    force_time = data[:, 0]
    user_force = -data[:, 1]
    desired_force = -data[:, 2]
    print("Data imported")

    font_size = 18

    # Create a figure and 2 subplots
    fig, ax = plt.subplots(ncols=1, nrows=2, gridspec_kw={
                           "height_ratios": [9, 1]})
    fig.canvas.mpl_connect("key_press_event", key_press)

    # Set figure height to screen height
    fig.set_size_inches(10 * 0.95, 7.5 * 0.95)

    desired_force_line, = ax[0].plot(
        [], [], "k-", label="Desired Force", linewidth=10, alpha=0.9, color="r")
    user_force_line, = ax[0].plot(
        [], [], "b-", label="User Force", linewidth=10, alpha=0.9, color="b")

    color = 16/255, 173/255, 210/255, 1
    # Set an rgb color for the background
    # fig.patch.set_facecolor(color)

    # # Set an rgb color for the axes
    # ax[0].set_facecolor(color)
    # ax[1].set_facecolor(color)

    # Set the window transparent using the PyQt5 backend



    # Create a legend with invisible box
    ax[0].legend(loc="upper left", fontsize=font_size, framealpha=0)

    loc = matplotlib.ticker.MultipleLocator(base=.5)
    ax[0].xaxis.set_major_locator(loc)
    loc = matplotlib.ticker.MultipleLocator(base=2)
    ax[0].yaxis.set_major_locator(loc)

    ax[0].set_xlim(0, force_time[min(SLIDE_LIMIT, len(force_time) - 1)])
    ax[0].set_ylim(-5, 5)

    # Increase the fontsize of the ticks
    ax[0].tick_params(axis="both", which="major", labelsize=font_size)


    ax[0].set_xlabel("Time (s)", fontsize=font_size)
    ax[0].set_ylabel("Force (N)", fontsize=font_size)

    # Grid on
    ax[0].grid(True)

    # Turn off the ticks on both axes
    ax[1].tick_params(axis="both", which="both", bottom=False, top=False,
                      labelbottom=False, right=False, left=False, labelleft=False)

    ax[1].spines['top'].set_visible(False)
    ax[1].spines['right'].set_visible(False)
    ax[1].spines['bottom'].set_visible(False)
    ax[1].spines['left'].set_visible(False)

    outerBox = plt.Rectangle((0.1, 0.1), 0.8, 0.8,
                             fc="w", fill=True, edgecolor='k', linewidth=2)
    innerBox = plt.Rectangle((0, 0.1), 0, 0.8,
                             fc="r", fill=True, edgecolor='none', linewidth=0)

    quarterLine = plt.Line2D((0.25, 0.25), (0.1, 0.9), color="k", linewidth=1)
    halfLine = plt.Line2D((0.5, 0.5), (0.1, 0.9), color="k", linewidth=1)
    threeQuarterLine = plt.Line2D(
        (0.75, 0.75), (0.1, 0.9), color="k", linewidth=1)

    ax[1].add_patch(outerBox)
    ax[1].add_patch(innerBox)
    ax[1].add_line(quarterLine)
    ax[1].add_line(halfLine)
    ax[1].add_line(threeQuarterLine)

    desired_force_line.set_linewidth(2)
    user_force_line.set_linewidth(2)
    plt.tight_layout()
    plt.subplots_adjust(hspace=0.2)
    
    # win = plt.gcf().canvas.manager.window
    # win.setWindowFlags(QtCore.Qt.FramelessWindowHint)
    # win.setAttribute(QtCore.Qt.WA_NoSystemBackground, True)
    # win.setAttribute(QtCore.Qt.WA_TranslucentBackground, True)
    # win.setStyleSheet("background:transparent")

    # win = plt.gcf().canvas.manager.window
    # win.lift()
    # win.attributes("-topmost", True)
    # win.attributes("-transparentcolor", "white")

    plt.show(block=False)

    max_force = max(abs(user_force))

    print("Press escape to exit, any other key to pause/play")
    N = len(force_time)
    # plt.pause(100)
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

        user_force_line.set_xdata(time_window)
        user_force_line.set_ydata(user_force_window)

        desired_force_line.set_xdata(time_window)
        desired_force_line.set_ydata(desired_force_window)

        max_force_window = max(max(user_force_window),
                               max(desired_force_window))
        min_force_window = min(min(user_force_window),
                               min(desired_force_window))

        ax[0].set_ylim(min(-5, min_force_window - abs(min_force_window) * 0.1),
                       max(5, max_force_window + abs(max_force_window) * 0.1))

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
        if user_force[i] > 0:
            innerBox.set_facecolor((1, 0, 0, width / 0.8 / 2 + 0.5))
        else:
            innerBox.set_facecolor((0, 1, 0, width / 0.8 / 2 + 0.5))
        pos_x = 0.1 + (0.8 - width) / 2
        innerBox.set_width(width)
        innerBox.set_x(pos_x)

        plt.pause(0.001)
    else:
        print("Press q to exit")
        global keep_window_open
        keep_window_open = True
        while plt.fignum_exists(fig.number) and keep_window_open:
            plt.pause(0.2)

if __name__ == '__main__':
    main()
