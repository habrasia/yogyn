import { ComponentFixture, TestBed } from '@angular/core/testing';
import { StudioListComponent } from './studio-list.component';

describe('StudioListComponent', () => {
  let component: StudioListComponent;
  let fixture: ComponentFixture<StudioListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StudioListComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(StudioListComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});