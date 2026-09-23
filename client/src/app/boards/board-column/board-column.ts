import { CdkDrag, CdkDragDrop, CdkDragHandle, CdkDropList } from '@angular/cdk/drag-drop';
import { Component, computed, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { TaskCard } from '../../tasks/task.models';
import { TaskCardComponent } from '../../tasks/task-card/task-card';
import { BoardColumn } from '../board.models';
import { columnDropId } from '../board.store';

@Component({
  selector: 'app-board-column',
  imports: [
    CdkDrag,
    CdkDragHandle,
    CdkDropList,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    TaskCardComponent,
  ],
  templateUrl: './board-column.html',
  styleUrl: './board-column.scss',
  host: { class: 'board-column' },
})
export class BoardColumnComponent {
  readonly column = input.required<BoardColumn>();
  readonly projectKey = input.required<string>();
  readonly canManage = input(false);
  readonly canEditTasks = input(false);
  readonly connectedTo = input<string[]>([]);

  readonly edit = output<BoardColumn>();
  readonly remove = output<BoardColumn>();
  readonly addTask = output<BoardColumn>();
  readonly openTask = output<TaskCard>();
  readonly taskDropped = output<CdkDragDrop<BoardColumn>>();

  protected readonly dropId = computed(() => columnDropId(this.column().id));
}
