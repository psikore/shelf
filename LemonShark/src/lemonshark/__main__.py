import sys
import time
import random
from itertools import count, product

from PyQt6 import QtCore, QtWidgets

class PacketModel(QtCore.QAbstractTableModel):
    def __init__(self):
        super().__init__()
        self._data = []

    def rowCount(self, parent = ...):
        return len(self._data)

    def columnCount(self, parent = ...):
        return 3    # Time, Source, Info

    def data(self, index, role=QtCore.Qt.ItemDataRole.DisplayRole):
        if role == QtCore.Qt.ItemDataRole.DisplayRole:
            return str(self._data[index.row()][index.column()])
        return None

    def addRows(self, rows):
        start = len(self._data)
        end = start + len(rows) - 1
        self.beginInsertRows(QtCore.QModelIndex(), start, end)
        self._data.extend(rows)
        self.endInsertRows()


class ProducerThread(QtCore.QThread):
    newRows = QtCore.pyqtSignal(list)

    def run(self):
        counter = 0
        while True:
            time.sleep(0.2)     # simulate packet capture delay

            counter += 1
            row = [f"{time.time()}:.3f",
                   f"192.168.0.{random.randint(1, 254)}",
                   f"Packet #{counter}"]
            self.newRows.emit([row])


class PacketViewer(QtWidgets.QTableView):
    def __init__(self, model):
        super().__init__()
        self.setModel(model)
        self.setVerticalScrollMode(QtWidgets.QAbstractItemView.ScrollMode.ScrollPerPixel)
        self.verticalHeader().setDefaultSectionSize(20)
        self.horizontalHeader().setStretchLastSection(True)


def main():
    app = QtWidgets.QApplication(sys.argv)
    model = PacketModel()
    viewer = PacketViewer(model)
    viewer.resize(600, 400)
    viewer.show()

    producer = ProducerThread()
    producer.newRows.connect(model.addRows)
    producer.start()

    sys.exit(app.exec())


if __name__ == "__main__":
    main()
