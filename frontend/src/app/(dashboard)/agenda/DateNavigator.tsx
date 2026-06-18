import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { ChevronLeft, ChevronRight, Calendar as CalendarIcon } from 'lucide-react';

interface DateNavigatorProps {
  currentDate: Date;
  onChangeDate: (date: Date) => void;
  category: string;
  onChangeCategory: (category: string) => void;
  instructorId?: string;
  onChangeInstructor?: (id: string) => void;
}

export function DateNavigator({ currentDate, onChangeDate, category, onChangeCategory }: DateNavigatorProps) {
  const minDate = new Date();
  minDate.setDate(minDate.getDate() - 28); // -4 weeks

  const maxDate = new Date();
  maxDate.setDate(maxDate.getDate() + 28); // +4 weeks

  const handlePrevWeek = () => {
    const newDate = new Date(currentDate);
    newDate.setDate(newDate.getDate() - 7);
    if (newDate >= minDate) {
      onChangeDate(newDate);
    }
  };

  const handleNextWeek = () => {
    const newDate = new Date(currentDate);
    newDate.setDate(newDate.getDate() + 7);
    if (newDate <= maxDate) {
      onChangeDate(newDate);
    }
  };

  const isPrevDisabled = new Date(currentDate).setDate(currentDate.getDate() - 7) < minDate.getTime();
  const isNextDisabled = new Date(currentDate).setDate(currentDate.getDate() + 7) > maxDate.getTime();

  const handleToday = () => {
    onChangeDate(new Date());
  };

  const formattedDate = new Intl.DateTimeFormat('pt-BR', { month: 'long', year: 'numeric' }).format(currentDate);
  const capitalizedDate = formattedDate.charAt(0).toUpperCase() + formattedDate.slice(1);

  return (
    <div className="flex items-center justify-between mb-4 bg-card p-4 rounded-lg border shadow-sm">
      <div className="flex items-center gap-2">
        <Button variant="outline" size="icon" onClick={handlePrevWeek} disabled={isPrevDisabled}>
          <ChevronLeft className="h-4 w-4" />
        </Button>
        <Button variant="outline" onClick={handleToday}>
          Hoje
        </Button>
        <Button variant="outline" size="icon" onClick={handleNextWeek} disabled={isNextDisabled}>
          <ChevronRight className="h-4 w-4" />
        </Button>
        <div className="flex items-center gap-2 ml-4 font-semibold text-lg">
          <CalendarIcon className="h-5 w-5 text-muted-foreground" />
          {capitalizedDate}
        </div>
      </div>
      <div className="flex gap-4">
        <Select value={category} onValueChange={onChangeCategory}>
          <SelectTrigger className="w-[180px]">
            <SelectValue placeholder="Categoria CNH" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="A">Categoria A (Moto)</SelectItem>
            <SelectItem value="B">Categoria B (Carro)</SelectItem>
            <SelectItem value="C">Categoria C (Caminhão)</SelectItem>
            <SelectItem value="D">Categoria D (Ônibus)</SelectItem>
            <SelectItem value="E">Categoria E (Carreta)</SelectItem>
            <SelectItem value="AB">Categoria AB</SelectItem>
          </SelectContent>
        </Select>
      </div>
    </div>
  );
}
