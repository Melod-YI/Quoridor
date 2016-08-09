package model.impl;

import model.data.StatisticData;
import model.impl.BaseModel;
import model.service.StatisticModelService;
import model.state.GameResultState;

public class StatisticModelImpl extends BaseModel implements StatisticModelService{

private StatisticData statisticDao;
	
	public StatisticModelImpl(){
		//初始化Dao
	}

	@Override
	public void recordStatistic(GameResultState result, int time) {
		// TODO Auto-generated method stub
		
	}

	@Override
	public void showStatistics() {
		// TODO Auto-generated method stub
		
	}

	public StatisticData getStatisticDao() {
		return statisticDao;
	}

	public void setStatisticDao(StatisticData statisticDao) {
		this.statisticDao = statisticDao;
	}

}
